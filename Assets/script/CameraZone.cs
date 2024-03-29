﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
  public static List<CameraZone> All = new List<CameraZone>();

  public int priority;
  [Tooltip("The camera will increase its size to view all colliders. Useful for rooms.")]
  public bool EncompassBounds;
  [Tooltip( "The camera will stay within the shapes of these colliders when the zone is active." )]
  public Collider2D[] colliders;

  public Bounds CameraBounds;

  private void Awake()
  {
    All.Add( this );
    UpdateBounds();
  }

  private void OnDestroy()
  {
    All.Remove( this );
  }

  void UpdateBounds()
  {
    foreach( var cld in colliders )
    {
      if( cld is PolygonCollider2D )
      {
        PolygonCollider2D poly = cld as PolygonCollider2D;
        // camera poly bounds points are local to polygon
        CameraBounds = new Bounds();
        foreach( var p in poly.points )
          CameraBounds.Encapsulate( p );
      }
      else if( cld is BoxCollider2D )
      {
        CameraBounds = (cld as BoxCollider2D).bounds;
      }
    }
  }

  public static bool DoesOverlapAnyZone( Vector2 point, ref CameraZone active )
  {
    for( int z = 0; z < All.Count; z++ )
    {
      for( int i = 0; i < All[z].colliders.Length; i++ )
      {
        if( All[z].colliders[i].OverlapPoint( point ) )
        {
          active = All[z];
          return true;
        }
      }
    }
    active = null;
    return false;
  }
}
