﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Ability : ScriptableObject
{
  public GameObject activatePrefab;
  GameObject go;
  public void Activate( PlayerBiped pc )
  {
    //Ability
    go = Instantiate( activatePrefab, pc.armMount.position, Quaternion.identity, pc.armMount );
    go.transform.localRotation = Quaternion.identity;
  }
  public void Deactivate()
  {
    Destroy( go );
  }
}