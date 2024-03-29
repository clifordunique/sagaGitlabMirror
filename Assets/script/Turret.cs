﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : Entity
{
  [Header( "Turret" )]
  [SerializeField] Transform cannon;
  [SerializeField] float sightRange = 6;
  [SerializeField] float small = 0.1f;
  [SerializeField] float rotspeed = 20;
  Timer shootRepeatTimer = new Timer();
  [SerializeField] Transform shotOrigin;
  [SerializeField] Weapon weapon;
  [SerializeField] Transform sightOrigin;
  [SerializeField] float sightStartRadius = 0.5f;

  [SerializeField] float min = -90;
  [SerializeField] float max = 90;
  public float maxShootAngle = 5;

  protected override void Start()
  {
    base.Start();
    UpdateLogic = UpdateTurret;
    UpdatePosition = null;
    UpdateCollision = null;
  }

  void UpdateTurret()
  {
    if( Global.instance.CurrentPlayer != null )
    {
      Vector2 pos = cannon.position;
      Vector2 player = Global.instance.CurrentPlayer.transform.position;
      Vector2 delta = player - pos;
      if( delta.sqrMagnitude < sightRange * sightRange )
      {
        Transform target = null;
        RaycastHit2D hit = Physics2D.Linecast( (Vector2)sightOrigin.position + delta.normalized * sightStartRadius, player, Global.TurretSightLayers );
        if( hit.transform.root == Global.instance.CurrentPlayer.transform )
          target = hit.transform;
        if( target == null )
        {
          cannon.rotation = Quaternion.RotateTowards( cannon.rotation, Quaternion.Euler( 0, 0, 0 ) * transform.localToWorldMatrix.rotation, rotspeed * Time.deltaTime );
          animator.Play( "idle" );
        }
        else
        {
          animator.Play( "alert" );
          Vector2 local = transform.worldToLocalMatrix.MultiplyVector( delta );
          // todo prevent cannon from rotating outside of 180 degree range 
          float angle = Mathf.Clamp( Util.NormalizeAngle( Mathf.Rad2Deg * Mathf.Atan2( local.y, local.x ) - 90 ), min, max );
          cannon.rotation = Quaternion.RotateTowards( cannon.rotation, Quaternion.Euler( 0, 0, angle ) * transform.localToWorldMatrix.rotation, rotspeed * Time.deltaTime );
          Vector2 aim = cannon.transform.up;
          if( target != null && target.IsChildOf( Global.instance.CurrentPlayer.transform ) )
          {
            if( Vector2.Angle( delta, aim ) < maxShootAngle )
              if( !shootRepeatTimer.IsActive )
                Shoot( aim );
          }
        }

      }
      else
      {
        animator.Play( "idle" );
      }
    }
  }

  void Shoot( Vector3 shoot )
  {
    shootRepeatTimer.Start( weapon.shootInterval, null, null );
    Vector3 pos = shotOrigin.position;
    //if( !Physics2D.Linecast( transform.position, pos, Global.ProjectileNoShootLayers ) )
    weapon.FireWeapon( this, pos, shoot );
  }
}
