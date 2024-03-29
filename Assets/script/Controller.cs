﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : ScriptableObject
{
  public static List<Controller> All = new List<Controller>();

  // input state is modified and passed to pawn
  protected InputState input = new InputState();
  public Pawn pawn;
  //public List<Pawn> minions = new List<Pawn>();
  public bool CursorInfluence;

  public virtual void Awake()
  {
    All.Add( this );
  }
  private void OnDestroy()
  {
    if( Global.IsQuiting )
      return;
    All.Remove( this );
  }

  public virtual void Update() { }

  public virtual void AssignPawn( Pawn pwn )
  {
    if( pawn!= null )
    {
      pawn.OnControllerUnassigned();
      pawn.controller = null;
    }
    pawn = pwn;
    if( pawn != null )
    {
      pawn.controller = this;
      pawn.OnControllerAssigned();
    }
  }

  public Pawn GetPawn()
  {
    return pawn;
  }

  //public void AddMinion( Pawn pawn )
  //{
  //  minions.Add( pawn );
  //}

  // For temporary things like running a short distance.
  // Otherwise, write a different controller for the pawn and assign that instead.
  public ref InputState GetInput()
  {
    return ref input;
  }
}
