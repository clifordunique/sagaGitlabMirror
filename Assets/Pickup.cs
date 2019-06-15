﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
  [SerializeField] Animator animator;
  public Weapon weapon;

  public void Highlight()
  {
    animator.Play( "highlight" );
  }
  public void Unhighlight()
  {
    animator.Play( "idle" );
  }

  void Start()
  {
    animator.Play( "idle" );
  }


}
