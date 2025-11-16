using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
	public class Hover : MonoBehaviour
	{

		// Use this for initialization
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			transform.localPosition = new Vector3(0, Mathf.Sin(Time.realtimeSinceStartup * 1.2f) * 0.1f, 0);
		}
	}
}