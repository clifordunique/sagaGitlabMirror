using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO dash smoke, dash sound
public class Mech : Character
{
  new public AudioSource audio;

  public bool facingRight;
  public BoxCollider2D fist;
  public BoxCollider2D torso;

  [SerializeField] float sightRange = 6;
  [SerializeField] float withinpunchRange = 3;
  [SerializeField] float jumpRange = 2;
  [SerializeField] float small = 0.1f;
  [SerializeField] float moveSpeed = 0.5f;
  [SerializeField] float jumpSpeed = 5;
  [SerializeField] float dashSpeed = 5;
  [SerializeField] float shootInterval = 1;
  [SerializeField] float shootUp = 5;
  // durations
  public float jumpDuration = 0.4f;
  public float dashDuration = 1;
  public float landDuration = 0.1f;
  bool onGround;
  Timer jumpTimer = new Timer();
  Timer jumpRepeat = new Timer();
  Timer dashTimer = new Timer();
  Timer dashCooldownTimer = new Timer();
  Timer shootRepeatTimer = new Timer();

  public AudioClip soundJump;
  public AudioClip soundReflect;

  [SerializeField] Weapon weapon;
  [SerializeField] Transform shotOrigin;

  // ignore damage below this value
  [SerializeField] float DamageThreshold = 2;

  void Start()
  {
    CharacterStart();
    UpdateLogic = BossUpdate;
    UpdateHit = MechHit;
    Physics2D.IgnoreCollision( box, fist );
    Physics2D.IgnoreCollision( box, torso );
    Physics2D.IgnoreCollision( torso, fist );
  }

  void BossUpdate()
  {
    if( velocity.y < 0 )
      StopJump();

    if( Global.instance.CurrentPlayer == null )
      return;

    Vector3 targetpos = Global.instance.CurrentPlayer.transform.position;
    Vector3 delta = targetpos - transform.position;

    //  do not change directions while dashing
    if( !dashTimer.IsActive )
      facingRight = delta.x >= 0;

    if( collideBottom && delta.sqrMagnitude < sightRange * sightRange )
    {
      if( !dashTimer.IsActive && !jumpTimer.IsActive )
      {
        if( delta.y > 1f && delta.y < 5 && dashCooldownTimer.ProgressNormalized > 0.5f && !jumpRepeat.IsActive && !jumpTimer.IsActive && delta.sqrMagnitude < jumpRange * jumpRange )
        {
          StartJump();
        }
        else if( !dashCooldownTimer.IsActive && delta.y > 0f && delta.y < 2 && Mathf.Abs( delta.x ) < withinpunchRange )
        {
          animator.Play( "punchdash" );
          StartDash();
        }
        else if( Mathf.Abs( delta.x ) < small )
        {
          animator.Play( "idle" );
          velocity.x = 0;
        }
        else
        {
          animator.Play( "walk" );
          velocity.x = Mathf.Sign( delta.x ) * moveSpeed;
          if( weapon != null && !shootRepeatTimer.IsActive )
          {
            // todo check line of site to target
            // todo check for team allegiance
            // todo have a "ready to fire" animation play to warn player
            //hit = Physics2D.LinecastAll( shotOrigin.position, player );
            hit = Physics2D.Linecast( shotOrigin.position, targetpos, LayerMask.GetMask( new string[] { "Default", "character" } ) );
            if( hit.transform == Global.instance.CurrentPlayer.transform )
              Shoot( targetpos - shotOrigin.position + Vector3.up * shootUp );
          }
        }
      }
    }


    transform.localScale = new Vector3( facingRight ? 1 : -1, 1, 1 );

    bool oldGround = onGround;
    onGround = collideBottom || (collideLeft && collideRight);
    if( onGround && !oldGround )
    {
      //landTimer.Start( landDuration, null, null )
    }
  }

  void StartJump()
  {
    velocity.y = jumpSpeed;
    audio.PlayOneShot( soundJump );
    //dashSmoke.Stop();
    animator.Play( "jump" );
    jumpRepeat.Start( 2, null, null );
    jumpTimer.Start( jumpDuration, null, delegate { StopJump(); } );
  }

  void StopJump()
  {
    jumpTimer.Stop( false );
    velocity.y = Mathf.Min( velocity.y, 0 );
  }

  void StartDash()
  {
    dashTimer.Start( dashDuration, ( x ) => {
      velocity.x = (facingRight ? 1 : -1) * dashSpeed;
    }, delegate
    {
      dashCooldownTimer.Start( 2 );
    } );
  }

  void StopDash()
  {
    dashTimer.Stop( false );
  }

  void Shoot( Vector3 shoot )
  {
    shootRepeatTimer.Start( shootInterval, null, null );
    if( !Physics2D.Linecast( transform.position, shotOrigin.position, Global.ProjectileNoShootLayers ) )
      weapon.FireWeapon( this, shotOrigin.position, shoot );
  }


  void MechHit()
  {
    if( ContactDamage == null )
      return;
    // body hit
    hitCount = Physics2D.BoxCastNonAlloc( body.position, box.size, 0, velocity, RaycastHits, raylength, Global.CharacterDamageLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      IDamage dam = hit.transform.GetComponent<IDamage>();
      if( dam != null )
      {
        Damage dmg = Instantiate( ContactDamage );
        dmg.damageSource = transform;
        dmg.point = hit.point;
        dam.TakeDamage( dmg );
      }
    }

    hitCount = Physics2D.BoxCastNonAlloc( torso.transform.position, torso.size, 0, velocity, RaycastHits, raylength, Global.CharacterDamageLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      IDamage dam = hit.transform.GetComponent<IDamage>();
      if( dam != null )
      {
        Damage dmg = Instantiate( ContactDamage );
        dmg.damageSource = transform;
        dmg.point = hit.point;
        dam.TakeDamage( dmg );
      }
    }

    // fist hit
    hitCount = Physics2D.BoxCastNonAlloc( fist.transform.position, fist.size, 0, Vector2.zero, RaycastHits, raylength, Global.CharacterDamageLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      IDamage dam = hit.transform.GetComponent<IDamage>();
      if( dam != null )
      {
        Damage dmg = Instantiate( ContactDamage );
        dmg.damageSource = transform;
        dmg.point = hit.point;
        dam.TakeDamage( dmg );
      }
    }
  }


  public override bool TakeDamage( Damage d )
  {
    if( d.amount < DamageThreshold )
    {
      if( soundReflect != null )
        audio.PlayOneShot( soundReflect );

      Projectile projectile = d.damageSource.GetComponent<Projectile>();
      if( projectile != null )
      {
        switch( projectile.weapon.weaponType )
        {
          case Weapon.WeaponType.Projectile:
          //projectile.transform.position = transform.position + Vector3.Project( (Vector3)d.point - transform.position, transform.right );
          projectile.velocity = Vector3.Reflect( projectile.velocity, (d.instigator.transform.position - transform.position).normalized );
          Physics2D.IgnoreCollision( projectile.circle, box, false );
          Physics2D.IgnoreCollision( projectile.circle, torso, false );
          Physics2D.IgnoreCollision( projectile.circle, fist, false );

          foreach( var cldr in projectile.instigator.IgnoreCollideObjects )
          {
            if( cldr == null )
              Debug.Log( "ignorecolideobjects null" );
            else
              Physics2D.IgnoreCollision( projectile.circle, cldr, false );
          }

          projectile.instigator = this;
          projectile.ignore.Add( transform );
          break;

          case Weapon.WeaponType.Laser:
          // create second beam
          break;
        }
      }
      return false;
    }
    return base.TakeDamage( d );
  }
}
