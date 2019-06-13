﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : Character
{
  new public AudioSource audio;

  public bool facingRight;
  SpriteChunk[] sac;
  public BoxCollider2D fist;
  public BoxCollider2D torso;

  [SerializeField] float sightRange = 6;
  [SerializeField] float jumpRange = 2;
  [SerializeField] float small = 0.1f;
  [SerializeField] float moveSpeed = 2;
  [SerializeField] float jumpSpeed = 5;
  [SerializeField] float dashSpeed = 5;
  // durations
  public float jumpDuration = 0.4f;
  public float dashDuration = 1;
  public float landDuration = 0.1f;
  [SerializeField] bool jumping;
  [SerializeField] bool onGround;
  [SerializeField] bool landing;
  float jumpStart;
  float landStart;
  Timer jumpRepeat = new Timer();
  public AudioClip soundJump;

  [SerializeField] Weapon weapon;
  Timer shootRepeatTimer = new Timer();
  [SerializeField] Transform shotOrigin;

  private void Awake()
  {
    sac = GetComponentsInChildren<SpriteChunk>();
    Physics2D.IgnoreCollision( box, fist );
    Physics2D.IgnoreCollision( box, torso );
    Physics2D.IgnoreCollision( torso, fist );
  }

  void Start()
  {
    CharacterStart();
    UpdateLogic = BossUpdate;
  }

  void BossUpdate()
  {

    if( velocity.y < 0 )
      jumping = false;

    if( jumping && (Time.time - jumpStart >= jumpDuration) )
      StopJump();

    if( landing && (Time.time - landStart >= landDuration) )
      landing = false;

    if( Global.instance.CurrentPlayer == null )
      return;
    Vector3 player = Global.instance.CurrentPlayer.transform.position;
    Vector3 tpos = player;
    Vector3 delta = tpos - transform.position;
    facingRight = delta.x >= 0;
    if( (player - transform.position).sqrMagnitude < sightRange * sightRange )
    {
      if( weapon != null )
        if( !shootRepeatTimer.IsActive )
          Shoot( player - shotOrigin.position );

      if( Mathf.Abs( delta.x ) < small )
      {
        animator.Play( "idle" );
        velocity.x = 0;
      }
      else
      {
        if( collideBottom )
        {
          if( delta.y > 1 && !jumpRepeat.IsActive && !jumping && delta.sqrMagnitude < jumpRange * jumpRange )
          {
            StartJump();
          }
          else
          {
            animator.Play( "walk" );
            velocity.x = Mathf.Sign( delta.x ) * moveSpeed;
          }
        }
      }
    }

    transform.localScale = new Vector3( facingRight ? 1 : -1, 1, 1 );

    bool oldGround = onGround;
    onGround = collideBottom || (collideLeft && collideRight);
    if( onGround && !oldGround )
    {
      //StopDash();
      landing = true;
      landStart = Time.time;
    }
  }

  void StartJump()
  {
    jumping = true;
    jumpStart = Time.time;
    velocity.y = jumpSpeed;
    audio.PlayOneShot( soundJump );
    //dashSmoke.Stop();
    animator.Play( "jump" );
    jumpRepeat.Start( 2, null, null );
  }

  void StopJump()
  {
    jumping = false;
    velocity.y = Mathf.Min( velocity.y, 0 );
  }


  void Shoot( Vector3 shoot )
  {
    shootRepeatTimer.Start( weapon.shootInterval, null, null );
    if( !Physics2D.Linecast( transform.position, shotOrigin.position, LayerMask.GetMask( Projectile.NoShootLayers ) ) )
      weapon.FireWeapon( this, shotOrigin.position, shoot );
  }

}
