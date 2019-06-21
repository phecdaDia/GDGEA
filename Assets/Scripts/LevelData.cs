﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData : MonoBehaviour
{
	public float encounterChance = 0.1f;

	private void Awake()
	{
		GameManager.Instance.LevelData = this;
	}
}