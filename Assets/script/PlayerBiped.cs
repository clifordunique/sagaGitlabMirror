﻿using System.Collections.Generic;
using UnityEngine;

public class PlayerBiped : Pawn
{
  public AudioSource audio;
  public AudioSource audio2;
  public ParticleSystem dashSmoke;
  public GameObject dashflashPrefab;
  public GameObject walljumpEffect;
  [SerializeField] Transform WallkickPosition;
  public Transform arm;

  const float raydown = 0.2f;
  const float downOffset = 0.12f;
  // smaller head box allows for easier jump out and up onto wall from vertically-aligned ledge.
  public Vector2 headbox = new Vector2( .1f, .1f );
  const float headboxy = -0.1f;
  const float downslopefudge = 0.2f;
  const float corner = 0.707f;

  [Header( "Setting" )]
  public float speedFactorNormalized = 1;

  // movement
  public float moveVelMin = 1.5f;
  public float moveVelMax = 3;
  public float moveSpeed { get { return moveVelMin + (moveVelMax - moveVelMin) * speedFactorNormalized; } }
  public float jumpVelMin = 5;
  public float jumpVelMax = 10;
  public float jumpSpeed { get { return jumpVelMin + (jumpVelMax - jumpVelMin) * speedFactorNormalized; } }
  public float jumpDuration = 0.4f;
  public float jumpRepeatInterval = 0.1f;
  public float dashVelMin = 3;
  public float dashVelMax = 10;
  public float dashSpeed { get { return dashVelMin + (jumpVelMax - jumpVelMin) * speedFactorNormalized; } }
  public float dashDuration = 1;
  public float wallJumpPushVelocity = 1.5f;
  public float wallJumpPushDuration = 0.1f;
  public float wallSlideFactor = 0.5f;
  public float landDuration = 0.1f;

  Vector2 shoot;

  [Header( "State" )]
  [SerializeField] bool facingRight = true;
  [SerializeField] bool onGround;
  [SerializeField] bool jumping;
  [SerializeField] bool walljumping;
  [SerializeField] bool wallsliding;
  [SerializeField] bool landing;
  [SerializeField] bool dashing;

  Vector3 hitBottomNormal;
  Vector2 wallSlideNormal;
  Timer dashTimer = new Timer();
  Timer jumpTimer = new Timer();
  Timer jumpRepeatTimer = new Timer();
  Timer landTimer = new Timer();
  Timer walljumpTimer = new Timer();

  [Header( "Cursor" )]
  // public GameObject InteractIndicator;
  [SerializeField] Transform Cursor;
  public Transform CursorSnapped;
  public Transform CursorAutoAim;
  // public Vector2 CursorWorldPosition;
  public float CursorScale = 2;
  public float SnapAngleDivide = 8;
  public float SnapCursorDistance = 1;
  public float AutoAimCircleRadius = 1;
  public float AutoAimDistance = 5;
  public float DirectionalMinimum = 0.3f;
  bool CursorAboveMinimumDistance;
  [SerializeField] LineRenderer lineRenderer;

  [Header( "Weapon" )]
  public Weapon weapon;
  public int CurrentWeaponIndex;
  [SerializeField] List<Weapon> weapons;
  Timer shootRepeatTimer = new Timer();
  ParticleSystem chargeEffect = null;
  public float chargeMin = 0.3f;
  public float armRadius = 0.3f;
  Timer chargePulse = new Timer();
  Timer chargeStartDelay = new Timer();
  public float chargeDelay = 0.2f;
  public bool chargePulseOn = true;
  public float chargePulseInterval = 0.1f;
  public Color chargeColor = Color.white;
  public Transform armMount;
  [SerializeField] Ability secondary;
  float chargeStart;
  GameObject chargeEffectGO;

  [Header( "Sound" )]
  public AudioClip soundJump;
  public AudioClip soundDash;
  public AudioClip soundDamage;

  [Header( "Damage" )]
  [SerializeField] float damageDuration = 0.5f;
  bool takingDamage;
  bool damagePassThrough;
  Timer damageTimer = new Timer();
  public Color damagePulseColor = Color.white;
  bool damagePulseOn;
  Timer damagePulseTimer = new Timer();
  public float damagePulseInterval = .1f;
  public float damageBlinkDuration = 1f;
  public float damageLift = 1f;
  public float damagePushAmount = 1f;

  [Header( "Graphook" )]
  [SerializeField] GameObject graphookTip;
  [SerializeField] SpriteRenderer grapCableRender;
  public float grapDistance = 10;
  public float grapSpeed = 5;
  public float grapTimeout = 5;
  public float grapPullSpeed = 10;
  public float grapStopDistance = 0.1f;
  Timer grapTimer = new Timer();
  Vector2 grapSize;
  Vector3 graphitpos;
  public bool grapShooting;
  public bool grapPulling;
  public AudioClip grapShotSound;
  public AudioClip grapHitSound;

  // cached for optimization
  int HitLayers;
  RaycastHit2D hitRight;
  RaycastHit2D hitLeft;

  // World Selection
  [SerializeField] float selectRange = 3;
  IWorldSelectable closestISelect;
  IWorldSelectable WorldSelection;
  List<Component> pups = new List<Component>();

  [SerializeField] GameObject SpiderbotPrefab;
  public SpiderPawn spider;

  protected override void Awake()
  {
    base.Awake();
    InitializeParts();
  }

  protected override void Start()
  {
    HitLayers = Global.TriggerLayers | Global.CharacterDamageLayers;
    IgnoreCollideObjects.AddRange( GetComponentsInChildren<Collider2D>() );
    spriteRenderers.AddRange( GetComponentsInChildren<SpriteRenderer>() );
    graphookTip.SetActive( false );
    grapCableRender.gameObject.SetActive( false );
    if( weapons.Count > 0 )
      weapon = weapons[CurrentWeaponIndex % weapons.Count];
    // unpack
    InteractIndicator.SetActive( false );
    InteractIndicator.transform.SetParent( null );
  }

  public override void OnControllerAssigned()
  {
    // settings are read before player is created, so set player settings here.
    speedFactorNormalized = Global.instance.FloatSetting["PlayerSpeedFactor"].Value;
    controller.CursorInfluence = Global.instance.BoolSetting["CursorInfluence"].Value;
  }

  public override void PreSceneTransition()
  {
    StopCharge();
    StopGrap();
    // "pack" the graphook with this gameobject
    // graphook is unpacked when used
    graphookTip.transform.parent = gameObject.transform;
    grapCableRender.transform.parent = gameObject.transform;
    InteractIndicator.transform.parent = gameObject.transform;
  }

  public override void PostSceneTransition()
  {
    InteractIndicator.SetActive( false );
    InteractIndicator.transform.SetParent( null );
  }

  protected override void OnDestroy()
  {
    if( Global.IsQuiting )
      return;
    base.OnDestroy();
    damageTimer.Stop( false );
    damagePulseTimer.Stop( false );
    shootRepeatTimer.Stop( false );
    if( chargeEffect != null )
    {
      audio2.Stop();
      Destroy( chargeEffectGO );
    }
    chargeStartDelay.Stop( false );
    chargePulse.Stop( false );
  }

  void AssignWeapon( Weapon wpn )
  {
    weapon = wpn;
    Global.instance.weaponIcon.sprite = weapon.icon;
    Cursor.GetComponent<SpriteRenderer>().sprite = weapon.cursor;
    StopCharge();
    foreach( var sr in spriteRenderers )
    {
      sr.material.SetColor( "_IndexColor", weapon.Color0 );
      sr.material.SetColor( "_IndexColor2", weapon.Color1 );
    }
  }

  void NextWeapon()
  {
    CurrentWeaponIndex = (CurrentWeaponIndex + 1) % weapons.Count;
    AssignWeapon( weapons[CurrentWeaponIndex] );
  }

  public override void UnselectWorldSelection()
  {
    if( WorldSelection != null )
    {
      WorldSelection.Unselect();
      WorldSelection = null;
    }
  }
  
  new void UpdateHit( float dT )
  {
    pups.Clear();
    hitCount = Physics2D.BoxCastNonAlloc( transform.position, box.size, 0, velocity, RaycastHits, Mathf.Max( raylength, velocity.magnitude * dT ), HitLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      if( hit.transform.IsChildOf( transform ) )
        continue;
      ITrigger tri = hit.transform.GetComponent<ITrigger>();
      if( tri != null )
      {
        tri.Trigger( transform );
      }
      IDamage dam = hit.transform.GetComponent<IDamage>();
      if( dam != null )
      {
        // maybe only damage other if charging weapon or has active powerup?
        if( ContactDamage != null )
        {
          Damage dmg = Instantiate( ContactDamage );
          dmg.instigator = this;
          dmg.damageSource = transform;
          dmg.point = hit.point;
          dam.TakeDamage( dmg );
        }
      }
    }

    pups.Clear();
    hitCount = Physics2D.CircleCastNonAlloc( transform.position, selectRange, Vector3.zero, RaycastHits, 0, Global.WorldSelectableLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      IWorldSelectable pup = hit.transform.GetComponent<IWorldSelectable>();
      if( pup != null )
      {
        pups.Add( (Component)pup );
        /*if( !highlightedPickups.Contains( pup ) )
        {
          pup.Highlight();
          highlightedPickups.Add( pup );
        }*/
      }
    }
    //WorldSelectable closest = (WorldSelectable)FindClosest( transform.position, pups.ToArray() );
    IWorldSelectable closest = (IWorldSelectable)Util.FindSmallestAngle( transform.position, shoot, pups.ToArray() );
    if( closest == null )
    {
      if( closestISelect != null )
      {
        closestISelect.Unhighlight();
        closestISelect = null;
      }
      if( WorldSelection != null )
      {
        WorldSelection.Unselect();
        WorldSelection = null;
      }
    }
    else if( closest != closestISelect )
    {
      if( closestISelect != null )
        closestISelect.Unhighlight();
      closestISelect = closest;
      closestISelect.Highlight();
    }
    /*highlightedPickupsRemove.Clear();
    foreach( var pup in highlightedPickups )
      if( !pups.Contains( pup ) )
      {
        pup.Unhighlight();
        highlightedPickupsRemove.Add( pup );
      }
    foreach( var pup in highlightedPickupsRemove )
      highlightedPickups.Remove( pup );
    */
  }

  new void UpdateCollision( float dT )
  {
    collideRight = false;
    collideLeft = false;
    collideTop = false;
    collideBottom = false;
    adjust = transform.position;

    #region multisampleAttempt
    /*
    // Avoid the (box-to-box) standing-on-a-corner-and-moving-means-momentarily-not-on-ground bug by 'sampling' the ground at multiple points
    RaycastHit2D right = Physics2D.Raycast( adjust + Vector2.right * box.x, Vector2.down, Mathf.Max( raylength, -velocity.y * Time.deltaTime ), LayerMask.GetMask( PlayerCollideLayers ) );
    RaycastHit2D left = Physics2D.Raycast( adjust + Vector2.left * box.x, Vector2.down, Mathf.Max( raylength, -velocity.y * Time.deltaTime ), LayerMask.GetMask( PlayerCollideLayers ) );
    if( right.transform != null )
      rightFoot = right.point;
    else
      rightFoot = adjust + ( Vector2.right * box.x ) + ( Vector2.down * Mathf.Max( raylength, -velocity.y * Time.deltaTime ) );
    if( left.transform != null )
      leftFoot = left.point;
    else
      leftFoot = adjust + ( Vector2.left * box.x ) + ( Vector2.down * Mathf.Max( raylength, -velocity.y * Time.deltaTime ) );

    if( right.transform != null || left.transform != null )
    {
      Vector2 across = rightFoot - leftFoot;
      Vector3 sloped = Vector3.Cross( across.normalized, Vector3.back );
      if( sloped.y > corner )
      {
        collideFeet = true;
        adjust.y = ( leftFoot + across * 0.5f ).y + contactSeparation + verticalOffset;
        hitBottomNormal = sloped;
      }
    }

    if( left.transform != null )
      Debug.DrawLine( adjust + Vector2.left * box.x, leftFoot, Color.green );
    else
      Debug.DrawLine( adjust + Vector2.left * box.x, leftFoot, Color.grey );
    if( right.transform != null )
      Debug.DrawLine( adjust + Vector2.right * box.x, rightFoot, Color.green );
    else
      Debug.DrawLine( adjust + Vector2.right * box.x, rightFoot, Color.grey );

    */
    #endregion

    float down = jumping ? raydown - downOffset : raydown;
    hitCount = Physics2D.BoxCastNonAlloc( adjust, box.size, 0, Vector2.down, RaycastHits, Mathf.Max( down, -velocity.y * dT ), Global.CharacterCollideLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      if( IgnoreCollideObjects.Contains( hit.collider ) )
        continue;
      if( hit.normal.y > corner )
      {
        collideBottom = true;
        adjust.y = hit.point.y + box.size.y * 0.5f + downOffset;
        hitBottomNormal = hit.normal;
        // moving platforms
        Entity cha = hit.transform.GetComponent<Entity>();
        if( cha != null )
        {

#if UNITY_EDITOR
        if( cha.GetInstanceID() == GetInstanceID() )
        {
          Debug.LogError( "character set itself as carry character", gameObject );
          Debug.Break();
        }
#endif  
          carryCharacter = cha;
        }
        break;
      }
    }

    hitCount = Physics2D.BoxCastNonAlloc( adjust + Vector2.down * headboxy, headbox, 0, Vector2.up, RaycastHits, Mathf.Max( raylength, velocity.y * dT ), Global.CharacterCollideLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      if( IgnoreCollideObjects.Contains( hit.collider ) )
        continue;
      if( hit.normal.y < -corner )
      {
        collideTop = true;
        adjust.y = hit.point.y - box.size.y * 0.5f - contactSeparation;
        break;
      }
    }

    hitCount = Physics2D.BoxCastNonAlloc( adjust, box.size, 0, Vector2.left, RaycastHits, Mathf.Max( contactSeparation, -velocity.x * dT ), Global.CharacterCollideLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      if( IgnoreCollideObjects.Contains( hit.collider ) )
        continue;
      if( hit.normal.x > corner )
      {
        collideLeft = true;
        hitLeft = hit;
        adjust.x = hit.point.x + box.size.x * 0.5f + contactSeparation;
        wallSlideNormal = hit.normal;
        break;
      }
    }

    hitCount = Physics2D.BoxCastNonAlloc( adjust, box.size, 0, Vector2.right, RaycastHits, Mathf.Max( contactSeparation, velocity.x * dT ), Global.CharacterCollideLayers );
    for( int i = 0; i < hitCount; i++ )
    {
      hit = RaycastHits[i];
      if( IgnoreCollideObjects.Contains( hit.collider ) )
        continue;
      if( hit.normal.x < -corner )
      {
        collideRight = true;
        hitRight = hit;
        adjust.x = hit.point.x - box.size.x * 0.5f - contactSeparation;
        wallSlideNormal = hit.normal;
        break;
      }
    }

    transform.position = adjust;
  }

  Vector2 GetShotOriginPosition()
  {
    return (Vector2)arm.position + shoot.normalized * armRadius;
  }

  void Shoot()
  {
    if( weapon == null || (weapon.HasInterval && shootRepeatTimer.IsActive) )
      return;
    if( !weapon.fullAuto )
      input.Fire = false;
    if( weapon.HasInterval )
      shootRepeatTimer.Start( weapon.shootInterval, null, null );
    Vector2 pos = GetShotOriginPosition();
    if( !Physics2D.Linecast( transform.position, pos, Global.ProjectileNoShootLayers ) )
      weapon.FireWeapon( this, pos, shoot );
  }

  void ShootCharged()
  {
    if( weapon == null || weapon.ChargeVariant == null )
    {
      StopCharge();
      return;
    }
    // hack: chargeEffect is used to indicate charged state
    if( chargeEffect != null )
    {
      audio.Stop();
      if( (Time.time - chargeStart) > chargeMin )
      {
        audio.PlayOneShot( weapon.soundChargeShot );
        Vector3 pos = GetShotOriginPosition();
        if( !Physics2D.Linecast( transform.position, pos, Global.ProjectileNoShootLayers ) )
          weapon.ChargeVariant.FireWeapon( this, pos, shoot );
      }
    }
    StopCharge();
  }

  void ShootGraphook()
  {
    if( grapShooting )
      return;
    if( grapPulling )
      StopGrap();
    Vector3 pos = GetShotOriginPosition();
    if( !Physics2D.Linecast( transform.position, pos, Global.ProjectileNoShootLayers ) )
    {
      RaycastHit2D hit = Physics2D.Raycast( pos, shoot, grapDistance, LayerMask.GetMask( new string[] { "Default", "triggerAndCollision", "enemy" } ) );
      if( hit )
      {
        //Debug.DrawLine( pos, hit.point, Color.red );
        grapShooting = true;
        graphitpos = hit.point;
        graphookTip.SetActive( true );
        graphookTip.transform.parent = null;
        graphookTip.transform.localScale = Vector3.one;
        graphookTip.transform.position = pos;
        graphookTip.transform.rotation = Quaternion.LookRotation( Vector3.forward, Vector3.Cross( Vector3.forward, (graphitpos - transform.position) ) ); ;
        grapTimer.Start( grapTimeout, delegate
        {
          pos = GetShotOriginPosition();
          graphookTip.transform.position = Vector3.MoveTowards( graphookTip.transform.position, graphitpos, grapSpeed * Time.deltaTime );
          //grap cable
          grapCableRender.gameObject.SetActive( true );
          grapCableRender.transform.parent = null;
          grapCableRender.transform.localScale = Vector3.one;
          grapCableRender.transform.position = pos;
          grapCableRender.transform.rotation = Quaternion.LookRotation( Vector3.forward, Vector3.Cross( Vector3.forward, (graphookTip.transform.position - pos) ) );
          grapSize = grapCableRender.size;
          grapSize.x = Vector3.Distance( graphookTip.transform.position, pos );
          grapCableRender.size = grapSize;

          if( Vector3.Distance( graphookTip.transform.position, graphitpos ) < 0.01f )
          {
            grapShooting = false;
            grapPulling = true;
            grapTimer.Stop( false );
            grapTimer.Start( grapTimeout, null, StopGrap );
            audio.PlayOneShot( grapHitSound );
          }
        },
        StopGrap );
        audio.PlayOneShot( grapShotSound );
      }
    }
  }

  void StopGrap()
  {
    grapShooting = false;
    grapPulling = false;
    graphookTip.SetActive( false );
    grapCableRender.gameObject.SetActive( false );
    // avoid grapsize.y == 0 if StopGrap is called before grapSize is assigned
    grapSize.y = grapCableRender.size.y;
    grapSize.x = 0;
    grapCableRender.size = grapSize;
    grapTimer.Stop( false );
  }

  void UpdateCursor()
  {
    Vector2 AimPosition = Vector2.zero;
    Vector2 cursorOrigin = arm.position;
    Vector2 cursorDelta = input.Aim;

    CursorAboveMinimumDistance = cursorDelta.magnitude > DirectionalMinimum;
    Cursor.gameObject.SetActive( CursorAboveMinimumDistance );

    if( Global.instance.AimSnap )
    {
      if( CursorAboveMinimumDistance )
      {
        // set cursor
        CursorSnapped.gameObject.SetActive( true );
        float angle = Mathf.Atan2( cursorDelta.x, cursorDelta.y ) / Mathf.PI;
        float snap = Mathf.Round( angle * SnapAngleDivide ) / SnapAngleDivide;
        Vector2 snapped = new Vector2( Mathf.Sin( snap * Mathf.PI ), Mathf.Cos( snap * Mathf.PI ) );
        AimPosition = cursorOrigin + snapped * SnapCursorDistance;
        CursorSnapped.position = AimPosition;
        CursorSnapped.rotation = Quaternion.LookRotation( Vector3.forward, snapped );
        CursorWorldPosition = cursorOrigin + cursorDelta;
      }
      else
      {
        CursorSnapped.gameObject.SetActive( false );
      }
    }
    else
    {
      CursorSnapped.gameObject.SetActive( false );
      AimPosition = cursorOrigin + cursorDelta;
      CursorWorldPosition = AimPosition;
    }

    if( Global.instance.AutoAim )
    {
      CursorWorldPosition = cursorOrigin + cursorDelta;
      RaycastHit2D[] hits = Physics2D.CircleCastAll( transform.position, AutoAimCircleRadius, cursorDelta, AutoAimDistance, LayerMask.GetMask( new string[] { "enemy" } ) );
      float distance = Mathf.Infinity;
      Transform closest = null;
      foreach( var hit in hits )
      {
        float dist = Vector2.Distance( CursorWorldPosition, hit.transform.position );
        if( dist < distance )
        {
          closest = hit.transform;
          distance = dist;
        }
      }

      if( closest == null )
      {
        CursorAutoAim.gameObject.SetActive( false );
      }
      else
      {
        CursorAutoAim.gameObject.SetActive( true );
        // todo adjust for flight path
        AimPosition = closest.position;
        CursorAutoAim.position = AimPosition;
      }
    }
    else
    {
      CursorAutoAim.gameObject.SetActive( false );
    }

    shoot = AimPosition - (Vector2)arm.position;
    // if no inputs override, then default to facing the aim direction
    if( shoot.sqrMagnitude < 0.0001f )
      shoot = facingRight ? Vector2.right : Vector2.left;
  }

  void Update()
  {
    if( Global.Paused )
      return;

    UpdateCursor();

    string anim = "idle";
    bool previousGround = onGround;
    onGround = collideBottom || (collideLeft && collideRight);
    if( onGround && !previousGround )
    {
      landing = true;
      landTimer.Start( landDuration, null, delegate { landing = false; } );
    }
    wallsliding = false;
    // must have input (or push) to move horizontally, so allow no persistent horizontal velocity (without push)
    if( grapPulling )
      velocity = Vector3.zero;
    else if( !(input.MoveRight || input.MoveLeft) )
      velocity.x = 0;

    if( carryCharacter != null )
      velocity = carryCharacter.velocity;

    // WEAPONS / ABILITIES
    if( input.Fire )
      Shoot();

    if( input.ChargeStart )
      StartCharge();

    if( input.ChargeEnd )
      ShootCharged();

    if( input.Graphook )
      ShootGraphook();

    if( input.NextWeapon )
      NextWeapon();
    //Shield.SetActive( input.Shield );

    if( input.MoveDown )
      hanging = false;

    if( input.Interact )
    {
      if( WorldSelection != null && WorldSelection != closestISelect )
        WorldSelection.Unselect();
      WorldSelection = closestISelect;
      if( WorldSelection != null )
      {
        WorldSelection.Select();
        if( WorldSelection is Pickup )
        {
          Pickup pickup = (Pickup)closestISelect;
          if( pickup.weapon != null )
          {
            if( !weapons.Contains( pickup.weapon ) )
            {
              weapons.Add( pickup.weapon );
              AssignWeapon( pickup.weapon );
            }
          }
          else if( pickup.ability != null )
          {
            secondary = pickup.ability;
            secondary.Activate( this );
            Destroy( pickup.gameObject );
          }
        }
      }
    }

    if( takingDamage )
    {
      velocity.y = 0;
    }
    else
    {
      if( input.MoveRight )
      {
        facingRight = true;
        // move along floor if angled downwards
        Vector3 hitnormalCross = Vector3.Cross( hitBottomNormal, Vector3.forward );
        if( onGround && hitnormalCross.y < 0 )
          // add a small downward vector for curved surfaces
          velocity = hitnormalCross * moveSpeed + Vector3.down * downslopefudge;
        else
          velocity.x = moveSpeed;
        if( !facingRight && onGround )
          StopDash();
      }

      if( input.MoveLeft )
      {
        facingRight = false;
        // move along floor if angled downwards
        Vector3 hitnormalCross = Vector3.Cross( hitBottomNormal, Vector3.back );
        if( onGround && hitnormalCross.y < 0 )
          // add a small downward vector for curved surfaces
          velocity = hitnormalCross * moveSpeed + Vector3.down * downslopefudge;
        else
          velocity.x = -moveSpeed;
        if( facingRight && onGround )
          StopDash();
      }

      if( input.DashStart && (onGround || collideLeft || collideRight) )
        StartDash();

      if( input.DashEnd && !jumping )
        StopDash();

      if( dashing )
      {
        if( onGround && !previousGround )
        {
          StopDash();
        }
        else if( facingRight )
        {
          if( onGround || input.MoveRight )
            velocity.x = dashSpeed;
          if( (onGround || collideRight) && !dashTimer.IsActive )
            StopDash();
        }
        else
        {
          if( onGround || input.MoveLeft )
            velocity.x = -dashSpeed;
          if( (onGround || collideLeft) && !dashTimer.IsActive )
            StopDash();
        }
      }

      if( onGround && input.JumpStart )
        StartJump();
      else
      if( input.JumpEnd )
        StopJump();
      else
      if( collideRight && input.MoveRight && hitRight.normal.y >= 0 )
      {
        if( input.JumpStart )
        {
          walljumping = true;
          velocity.y = jumpSpeed;
          Push( Vector2.left * (input.DashStart ? dashSpeed : wallJumpPushVelocity), wallJumpPushDuration );
          jumpRepeatTimer.Start( jumpRepeatInterval );
          walljumpTimer.Start( wallJumpPushDuration, null, delegate { walljumping = false; } );
          audio.PlayOneShot( soundJump );
          Instantiate( walljumpEffect, WallkickPosition.position, Quaternion.identity );
        }
        else if( !jumping && !walljumping && !onGround && velocity.y < 0 )
        {
          wallsliding = true;
          velocity.y += (-velocity.y * wallSlideFactor) * Time.deltaTime;
          dashSmoke.transform.localPosition = new Vector3( 0.2f, -0.2f, 0 );
          if( !dashSmoke.isPlaying )
            dashSmoke.Play();
        }
      }
      else if( collideLeft && input.MoveLeft && hitLeft.normal.y >= 0 )
      {
        if( input.JumpStart )
        {
          walljumping = true;
          velocity.y = jumpSpeed;
          Push( Vector2.right * (input.DashStart ? dashSpeed : wallJumpPushVelocity), wallJumpPushDuration );
          jumpRepeatTimer.Start( jumpRepeatInterval );
          walljumpTimer.Start( wallJumpPushDuration, null, delegate { walljumping = false; } );
          audio.PlayOneShot( soundJump );
          Instantiate( walljumpEffect, WallkickPosition.position, Quaternion.identity );
        }
        else if( !jumping && !walljumping && !onGround && velocity.y < 0 )
        {
          wallsliding = true;
          velocity.y += (-velocity.y * wallSlideFactor) * Time.deltaTime;
          dashSmoke.transform.localPosition = new Vector3( 0.2f, -0.2f, 0 );
          if( !dashSmoke.isPlaying )
            dashSmoke.Play();
        }
      }
    }

    if( velocity.y < 0 )
    {
      jumping = false;
      walljumping = false;
      walljumpTimer.Stop( false );
    }

    if( !((onGround && dashing) || wallsliding) )
      dashSmoke.Stop();

    if( grapPulling )
    {
      Vector3 armpos = GetShotOriginPosition();
      grapCableRender.transform.position = armpos;
      grapCableRender.transform.rotation = Quaternion.LookRotation( Vector3.forward, Vector3.Cross( Vector3.forward, (graphookTip.transform.position - armpos) ) );
      grapSize = grapCableRender.size;
      grapSize.x = Vector3.Distance( graphookTip.transform.position, armpos );
      grapCableRender.size = grapSize;

      Vector3 grapDelta = graphitpos - transform.position;
      if( grapDelta.magnitude < grapStopDistance )
        StopGrap();
      else if( grapDelta.magnitude > 0.01f )
        velocity = grapDelta.normalized * grapPullSpeed;
    }

    // add gravity before velocity limits
    velocity.y -= Global.Gravity * Time.deltaTime;
    // grap force
    if( !grapPulling && pushTimer.IsActive )
      velocity.x = pushVelocity.x;

    if( hanging )
      velocity = Vector3.zero;

    // limit velocity before adding to position
    if( collideRight )
    {
      velocity.x = Mathf.Min( velocity.x, 0 );
      pushVelocity.x = Mathf.Min( pushVelocity.x, 0 );
    }
    if( collideLeft )
    {
      velocity.x = Mathf.Max( velocity.x, 0 );
      pushVelocity.x = Mathf.Max( pushVelocity.x, 0 );
    }
    // "onGround" is not the same as "collideFeet"
    if( onGround )
    {
      velocity.y = Mathf.Max( velocity.y, 0 );
      pushVelocity.x -= (pushVelocity.x * friction) * Time.deltaTime;
    }
    if( collideTop )
    {
      velocity.y = Mathf.Min( velocity.y, 0 );
    }
    velocity.y = Mathf.Max( velocity.y, -Global.MaxVelocity );

    transform.position += (Vector3)velocity * Time.deltaTime;
    carryCharacter = null;

    UpdateHit( Time.deltaTime );
    // update collision flags, and adjust position before render
    UpdateCollision( Time.deltaTime );

    if( health <= 0 )
      anim = "death";
    else if( takingDamage )
      anim = "damage";
    else if( walljumping )
      anim = "walljump";
    else if( jumping )
      anim = "jump";
    else if( wallsliding )
      anim = "wallslide";
    else if( onGround )
    {
      if( dashing )
        anim = "dash";
      else if( input.MoveRight || input.MoveLeft )
        anim = "run";
      else if( landing )
        anim = "land";
      else
      {
        anim = "idle";
        // when idling, always face aim direction
        facingRight = shoot.x >= 0;
      }
    }
    else if( !jumping )
      anim = "fall";

    Play( anim );
    transform.localScale = new Vector3( facingRight ? 1 : -1, 1, 1 );
    renderer.material.SetInt( "_FlipX", facingRight ? 0 : 1 );
    arm.localScale = new Vector3( facingRight ? 1 : -1, 1, 1 );
    arm.rotation = Quaternion.LookRotation( Vector3.forward, Vector3.Cross( Vector3.forward, shoot ) );
    if( wallsliding && collideRight )
      transform.rotation = Quaternion.LookRotation( Vector3.forward, new Vector3( wallSlideNormal.y, -wallSlideNormal.x ) );
    else if( wallsliding && collideLeft )
      transform.rotation = Quaternion.LookRotation( Vector3.forward, new Vector3( -wallSlideNormal.y, wallSlideNormal.x ) );
    else
      transform.rotation = Quaternion.Euler( 0, 0, 0 );

    ResetInput();
  }

  void LateUpdate()
  {
    if( !Global.instance.Updating )
      return;

    UpdateParts();

    Cursor.position = CursorWorldPosition;

    if( Global.instance.ShowAimPath && CursorAboveMinimumDistance )
    {
      lineRenderer.enabled = true;
      Vector3[] trail = weapon.GetTrajectory( GetShotOriginPosition(), shoot );
      lineRenderer.positionCount = trail.Length;
      lineRenderer.SetPositions( trail );
    }
    else
    {
      lineRenderer.enabled = false;
    }
  }

  void StartJump()
  {
    jumping = true;
    jumpTimer.Start( jumpDuration, null, StopJump );
    jumpRepeatTimer.Start( jumpRepeatInterval );
    velocity.y = jumpSpeed;
    audio.PlayOneShot( soundJump );
    dashSmoke.Stop();
  }

  void StopJump()
  {
    jumping = false;
    velocity.y = Mathf.Min( velocity.y, 0 );
    jumpTimer.Stop( false );
  }

  void StartDash()
  {
    if( !dashing )
    {
      dashing = true;
      dashTimer.Start( dashDuration );
      if( onGround )
        audio.PlayOneShot( soundDash, 0.5f );
      dashSmoke.transform.localPosition = new Vector3( -0.38f, -0.22f, 0 );
      dashSmoke.Play();
      if( onGround )
      {
        GameObject go = Instantiate( dashflashPrefab, transform.position + new Vector3( facingRight ? -0.25f : 0.25f, -0.25f, 0 ), Quaternion.identity );
        go.transform.localScale = facingRight ? Vector3.one : new Vector3( -1, 1, 1 );
      }
    }
  }

  void StopDash()
  {
    dashing = false;
    dashSmoke.Stop();
    dashTimer.Stop( false );
  }

  void StartCharge()
  {
    if( weapon == null )
      return;
    if( weapon.ChargeVariant != null )
    {
      chargeStartDelay.Start( chargeDelay, null, delegate
      {
        audio.PlayOneShot( weapon.soundCharge );
        audio2.clip = weapon.soundChargeLoop;
        audio2.loop = true;
        audio2.PlayScheduled( AudioSettings.dspTime + weapon.soundCharge.length );
        foreach( var sr in spriteRenderers )
          //sr.color = chargeColor;
          sr.material.SetColor( "_FlashColor", chargeColor );
        ChargePulseFlip();
        if( chargeEffectGO != null )
          Destroy( chargeEffectGO );
        chargeEffectGO = Instantiate( weapon.ChargeEffect, transform );
        chargeEffect = chargeEffectGO.GetComponent<ParticleSystem>();
        chargeStart = Time.time;
      } );
    }
  }

  void StopCharge()
  {
    if( chargeEffect != null )
    {
      audio2.Stop();
      Destroy( chargeEffectGO );
    }
    chargeStartDelay.Stop( false );
    chargePulse.Stop( false );
    foreach( var sr in spriteRenderers )
      sr.material.SetFloat( "_FlashAmount", 0 );
    chargeEffect = null;
  }

  void ChargePulseFlip()
  {
    chargePulse.Start( chargePulseInterval, null, delegate
    {
      chargePulseOn = !chargePulseOn;
      if( chargePulseOn )
        foreach( var sr in spriteRenderers )
          sr.material.SetFloat( "_FlashAmount", 1 );
      else
        foreach( var sr in spriteRenderers )
          sr.material.SetFloat( "_FlashAmount", 0 );
      ChargePulseFlip();
    } );
  }


  void DamagePulseFlip()
  {
    damagePulseTimer.Start( damagePulseInterval, null, delegate
    {
      damagePulseOn = !damagePulseOn;
      if( damagePulseOn )
        foreach( var sr in spriteRenderers )
          sr.enabled = true;
      else
        foreach( var sr in spriteRenderers )
          sr.enabled = false;
      DamagePulseFlip();
    } );
  }

  protected override void Die()
  {
    if( spider == null )
    {
      GameObject go = Instantiate( SpiderbotPrefab, transform.position, Quaternion.identity );
      spider = go.GetComponent<SpiderPawn>();
    }
    controller.AssignPawn( spider );
  }

  public override bool TakeDamage( Damage d )
  {
    if( !CanTakeDamage || damagePassThrough || health <= 0 )
      return false;
    if( d.instigator != null && !IsEnemyTeam( d.instigator.Team ) )
      return false;

    health -= d.amount;
    if( health <= 0 )
    {
      flashTimer.Stop( false );
      Die();
      return true;
    }

    partHead.transform.localScale = Vector3.one * (1 + (health / MaxHealth) * 10);
    audio.PlayOneShot( soundDamage );
    Global.instance.CameraController.GetComponent<CameraShake>().enabled = true;
    Play( "damage" );
    float sign = Mathf.Sign( d.damageSource.position.x - transform.position.x );
    facingRight = sign > 0;
    velocity.y = 0;
    Push( new Vector2( -sign * damagePushAmount, damageLift ), damageDuration );
    arm.gameObject.SetActive( false );
    takingDamage = true;
    damagePassThrough = true;
    StopGrap();
    damageTimer.Start( damageDuration, delegate ( Timer t )
    {
      //push.x = -sign * damagePushAmount;
    }, delegate ()
    {
      takingDamage = false;
      arm.gameObject.SetActive( true );
      DamagePulseFlip();
      damageTimer.Start( damageBlinkDuration, null, delegate ()
      {
        foreach( var sr in spriteRenderers )
          sr.enabled = true;
        damagePassThrough = false;
        damagePulseTimer.Stop( false );
      } );
    } );
    /*
    // color pulse
    flip = false;
    foreach( var sr in spriteRenderers )
      sr.material.SetFloat( "_FlashAmount", flashOn );
    flashTimer.Start( flashCount * 2, flashInterval, delegate ( Timer t )
    {
      flip = !flip;
      if( flip )
        foreach( var sr in spriteRenderers )
          sr.material.SetFloat( "_FlashAmount", flashOn );
      else
        foreach( var sr in spriteRenderers )
          sr.material.SetFloat( "_FlashAmount", 0 );
    }, delegate
    {
      foreach( var sr in spriteRenderers )
        sr.material.SetFloat( "_FlashAmount", 0 );
    } );
    */

    return true;
  }

  public void DoorTransition( bool right, float duration, float distance )
  {
    enabled = false;
    damageTimer.Stop( true );
    facingRight = right;
    transform.localScale = new Vector3( facingRight ? 1 : -1, 1, 1 );
    renderer.material.SetInt( "_FlipX", facingRight ? 0 : 1 );
    arm.localScale = new Vector3( facingRight ? 1 : -1, 1, 1 );
    SetAnimatorUpdateMode( AnimatorUpdateMode.UnscaledTime );
    Play( "run" );
    LerpToTarget lerp = gameObject.GetComponent<LerpToTarget>();
    if( lerp == null )
      lerp = gameObject.AddComponent<LerpToTarget>();
    lerp.moveTransform = transform;
    lerp.WorldTarget = true;
    lerp.targetPositionWorld = transform.position + ((right ? Vector3.right : Vector3.left) * distance);
    lerp.duration = duration;
    lerp.unscaledTime = true;
    lerp.enabled = true;
    lerp.OnLerpEnd = delegate
    {
      enabled = true;
      SetAnimatorUpdateMode( AnimatorUpdateMode.Normal );
    };
  }


  [Header( "Character Parts" )]
  [SerializeField] int CharacterLayer;

  [System.Serializable]
  public struct CharacterPart
  {
    public Transform transform;
    public Animator animator;
    public SpriteRenderer renderer;
    public int layerAnimated;
    //public BoxCollider2D box;
  }

  public CharacterPart partBody;
  public CharacterPart partHead;
  public CharacterPart partArmBack;
  public CharacterPart partArmFront;

  List<CharacterPart> CharacterParts;

  // Call from Awake()
  void InitializeParts()
  {
    CharacterParts = new List<CharacterPart> { partBody, partHead, partArmBack, partArmFront };
  }

  // Call from LateUpdate()
  void UpdateParts()
  {
    foreach( var part in CharacterParts )
      part.renderer.sortingOrder = CharacterLayer + part.layerAnimated;
  }

  void Play( string anim )
  {
    partBody.animator.Play( anim );
    /*
    foreach( var part in CharacterParts )
      if( part.animator != null && part.animator.HasState( 0, Animator.StringToHash( anim ) ) )
        part.animator.Play( anim );
        */
    //#if UNITY_EDITOR
    //      else
    //        Debug.LogWarning( "anim " + anim + " not found on " + part.transform.name );
    //#endif
  }

  void SetAnimatorUpdateMode( AnimatorUpdateMode mode )
  {
    // for changing the time scale to/from unscaled
    foreach( var part in CharacterParts )
      if( part.animator != null )
        part.animator.updateMode = mode;
  }

  void SetAnimatorSpeed( float speed )
  {
    foreach( var part in CharacterParts )
      if( part.animator != null )
        part.animator.speed = speed;
  }
}
