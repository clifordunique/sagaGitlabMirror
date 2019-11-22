﻿using UnityEngine;
using System.Collections;

public class BasicProjectile : Projectile, IDamage
{
  public float HitTimeout = 0.1f;
  public new Light light;
  public Vector2 constantAcceleration;
  Timer timeoutTimer;
  int HitCount;
  public int DieAfterHitCount;
  public bool AlignRotationToVelocity = true;

  void OnDestroy()
  {
    timeoutTimer.Stop( false );
  }

  void Start()
  {
    // let the weapon play the sound instead
    /*if( StartSound != null )
      Global.instance.AudioOneShot( StartSound, transform.position );*/

    timeoutTimer = new Timer( timeout, null, delegate ()
    {
      if( gameObject != null )
        Destroy( gameObject );
    } );
    if( AlignRotationToVelocity )
      transform.rotation = Quaternion.Euler( new Vector3( 0, 0, Mathf.Rad2Deg * Mathf.Atan2( velocity.normalized.y, velocity.normalized.x ) ) );
  }

  void Hit( Vector3 position )
  {
    transform.position = position;
    enabled = false;
    light.enabled = false;
    animator.Play( "hit" );
    timeoutTimer.Stop( false );
    timeoutTimer = new Timer( HitTimeout, null, delegate
    {
      if( gameObject != null )
        Destroy( gameObject );
    } );
  }

  void FixedUpdate()
  {
    if( AlignRotationToVelocity )
      transform.rotation = Quaternion.Euler( new Vector3( 0, 0, Mathf.Rad2Deg * Mathf.Atan2( velocity.normalized.y, velocity.normalized.x ) ) );

    RaycastHit2D hit = Physics2D.CircleCast( transform.position, circle.radius, velocity, raycastDistance, LayerMask.GetMask( Global.DefaultProjectileCollideLayers ) );
    if( hit.transform != null && (instigator == null || !hit.transform.IsChildOf( instigator )) )
    {
      IDamage dam = hit.transform.GetComponent<IDamage>();
      if( dam != null )
      {
        Damage dmg = Instantiate( ContactDamage );
        dmg.instigator = transform;
        dmg.point = hit.point;
        if( dam.TakeDamage( dmg ) )
        {
          HitCount++;
          if( HitCount >= DieAfterHitCount )
          {
            Hit( hit.point );
            return;
          }
        }
      }
      else
      {
        Hit( hit.point );
        return;
      }
    }
    //else
    {
      velocity += constantAcceleration * Time.fixedDeltaTime;
      transform.position += (Vector3)velocity * Time.fixedDeltaTime;
    }
    /*SpriteRenderer sr = GetComponent<SpriteRenderer>();
    sr.color = Global.instance.shiftyColor;
    light.color = Global.instance.shiftyColor;*/
  }

  public bool TakeDamage( Damage damage )
  {
    return true;
  }
}