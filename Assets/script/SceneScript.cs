﻿using UnityEngine;
using System.Collections;


public class SceneScript : MonoBehaviour
{
  public AudioLoop music;
  public CameraZone CameraZone;
  public BoxCollider NavmeshBox;

  public virtual void StartScene()
  {
    if( Application.isEditor && !Global.instance.SimulatePlayer && Global.instance.CurrentPlayer == null )
    {
      Global.instance.SpawnPlayer();
    }

    Global.instance.AssignCameraZone( CameraZone );

    if( music != null )
      Global.instance.PlayMusic( music );

  }

  public void PlayerInputOff()
  {
    Global.instance.Controls.BipedActions.Disable();
  }

  public void CameraZoom( float value )
  {
    Global.instance.CameraController.orthoTarget = value;
  }

  public void AssignCameraZone( CameraZone  zone )
  {
    Global.instance.AssignCameraZone( zone );
  }

  /*Timer runTimer = new Timer();
  public void RunLeft( float duration )
  {
    Global.instance.Controls.BipedActions.Disable();
    TimerParams tp = new TimerParams
    {
      unscaledTime = true,
      repeat = false,
      duration = duration,
      UpdateDelegate = delegate ( Timer t )
      {
        Global.instance.CurrentPlayer.inputLeft = true;
      },
      CompleteDelegate = delegate
      {
        Global.instance.Controls.BipedActions.Enable();
      }
    };
    runTimer.Start( tp );
  }

  public void RunRight( float duration )
  {
    Global.instance.Controls.BipedActions.Disable();
    TimerParams tp = new TimerParams
    {
      unscaledTime = true,
      repeat = false,
      duration = duration,
      UpdateDelegate = delegate ( Timer t )
      {
        Global.instance.CurrentPlayer.inputRight = true;
      },
      CompleteDelegate = delegate
      {
        Global.instance.Controls.BipedActions.Enable();
      }
    };
    runTimer.Start( tp );
  }*/

  

}