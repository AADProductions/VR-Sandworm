using UnityEngine;
using System.Collections;

public class Thumper : MonoBehaviour {

	public AnimationCurve ThumpCurve;
	public AudioClip ThumpClip;
	public float StopTime;
	public Transform Piston;
	public float ThumpPoint;
	public float ResetPoint;
	public ParticleSystem DustParticles;
	Vector3 pistonPos;
	bool thumped = false;

	void Start () {
		pistonPos = Piston.localPosition;
	}
	
	void Update () {
		if (Time.time > StopTime) {
			gameObject.SetActive (false);
			return;
		}
		pistonPos.y = ThumpCurve.Evaluate (Time.time);
		if (pistonPos.y < ThumpPoint && !thumped) {
			Thump ();
			thumped = true;
		} else if (pistonPos.y > ResetPoint) {
			thumped = false;
		}
		Piston.localPosition = pistonPos;
	}

	void Thump () {
		GetComponent <AudioSource> ().PlayOneShot (ThumpClip);
		DustParticles.Play ();
	}
}
