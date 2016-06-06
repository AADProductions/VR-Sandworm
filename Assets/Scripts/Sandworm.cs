using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Sandworm : MonoBehaviour {
	public float InitialDelay = 10f;
	[Range (0.0f, 0.65f)]
	public float NormalizedPosition;
	public float TotalTime = 720f;
	public float RoarTime = 10f;
	public float WormAnimationMultiplier;
	public Material [] BodySandMats;
	public Vector3 RandomAcceleration;
	public Vector3 RotationOffset;
	public BezierSpline Spline;
	public Transform Worm;
	public Transform [] TailPieces;
	public Transform StartNode;
	public AnimationClip SandwormAnimationClip;
	public Material SandwormMat;
	public AudioSource RumbleSource;
	public AnimationCurve RumbleCurve;
	public AnimationCurve ChurnedSandCurve;
	public AnimationCurve WormHeightCurve;
	public AnimationCurve MiniSprayCurve;
	public AnimationCurve InitialSprayCurve;
	public AnimationCurve InitialSprayYPos;
	public AnimationCurve FallingSandCurve;
	public AnimationCurve [] ParticleCurves;
	public ParticleSystem [] ParticleSystems;
	public float [] ParticleEmissionRates;
	public AnimationCurve FallingParticlesCurve;
	public Material ChurnedSandMat;
	public SkinnedMeshRenderer ParticleEmitter;
	public SkinnedMeshRenderer Dunes;
	public AnimationCurve[] DuneBlendShapes;
	public Transform [] SplineNodes;
	public Transform SplineNodeParent;
	public Transform UnderSprayParent;
	public Transform UnderSpray;
	public Vector3 UnderSprayRotationOffset;
	public Vector3 UnderSprayPositionOffset;
	public Kvant.Spray MiniSpray;
	public Kvant.Spray InitialSpray;
	public GameObject Spray;
	public GameObject SprayPrefab;
	Vector3 [] smoothPositions;
	Quaternion [] smoothRotations;
	Vector3 [] startPositions;
	Vector3 [] startRotations;
	Transform [] startParents;
	float [] startParams;
	float baseStartParam;
	Animation animation;
	Animation splineAnimation;
	public bool Suspend;
	float startTime;
	public Vector2 [] MainTextureOffset;
	public Vector2 [] DetailTextureOffset;

	void OnEnable () {
		//setting up the sandworm object
		//because the sandworm is constantly being changed in maya

		Spline.Reset ();
		List <Vector3> splinePoints = new List<Vector3> ();
		for (int i = 0; i < SplineNodes.Length; i++) {
			splinePoints.Add (SplineNodes [i].localPosition);
		}
		Spline.AddPoints (splinePoints.ToArray ());

		NormalizedPosition = 0f;
		GameObject sw = GameObject.Find ("Sandworm");
		#if UNITY_EDITOR
		UnityEditor.PrefabUtility.RevertPrefabInstance (sw);
		#endif
		Transform [] wormTransforms = sw.transform.GetComponentsInChildren <Transform> ();
		List <Transform> tailTransformsSorted = new List <Transform> ();
		foreach (Transform t in wormTransforms) {
			if (t.name == "Root") {
				Worm = t;
			} else if (t.name.Contains ("Tail_") && t.name != "Tail_39") {
				tailTransformsSorted.Add (t);
			}
		}
		tailTransformsSorted.Sort ((t1, t2) => t1.name.CompareTo(t2.name));
		TailPieces = tailTransformsSorted.ToArray ();

		ParticleEmitter = sw.transform.FindChild ("WormParticleEmitter").GetComponent <SkinnedMeshRenderer> ();
		ParticleEmitter.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		ParticleEmitter.quality = SkinQuality.Bone1;
		ParticleEmitter.gameObject.layer = 9;
		//disabling this for now
		ParticleEmitter.gameObject.SetActive (false);

		SkinnedMeshRenderer smr = sw.GetComponentInChildren <SkinnedMeshRenderer> ();
		smr.sharedMaterials = new Material[] { SandwormMat };//, BodySandMats [1] };
		smr.updateWhenOffscreen = true;

		startTime = Time.time;
		startRotations = new Vector3 [TailPieces.Length];
		startPositions = new Vector3 [TailPieces.Length];
		startParents = new Transform [TailPieces.Length];
		for (int i = 0; i < TailPieces.Length; i++) {
			startPositions [i] = TailPieces [i].localPosition;
			startRotations [i] = TailPieces [i].localEulerAngles;
			startParents [i] = TailPieces [i].parent;
		}

		Transform wormParent = Worm.parent;
		Worm.parent = StartNode;
		Worm.localPosition = Vector3.zero;
		Worm.localRotation = Quaternion.identity;
		Worm.parent = wormParent;
		startParams = new float [TailPieces.Length];
		for (int i = 0; i < TailPieces.Length; i++) {
			startParams [i] = Spline.GetAdjustedTimeAtPoint (TailPieces [i].position, 0.0001f, 0f, 1f, 0.0001f);
		}
		baseStartParam = Spline.GetAdjustedTimeAtPoint (Worm.position, 0.0001f, 0f, 1f, 0.0001f);

		animation = Worm.parent.GetComponent <Animation> ();
		if (animation == null) {
			animation = Worm.parent.gameObject.AddComponent <Animation> ();
			animation.playAutomatically = false;
		}
		animation.clip = SandwormAnimationClip;
		animation.AddClip (SandwormAnimationClip, "SandwormAnimation");
		animation.Play ("SandwormAnimation");
		animation ["SandwormAnimation"].enabled = true;
		animation ["SandwormAnimation"].normalizedSpeed = 0f;

		splineAnimation = GetComponent <Animation> ();
		splineAnimation.Play ("SplineAnimation");
		splineAnimation ["SplineAnimation"].enabled = true;
		splineAnimation ["SplineAnimation"].normalizedSpeed = 0f;

		GameObject swSandBody = GameObject.Find ("WormSand");
		SkinnedMeshRenderer swSandBodyRenderer = swSandBody.GetComponent <SkinnedMeshRenderer> ();
		swSandBodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		swSandBodyRenderer.useLightProbes = false;
		swSandBodyRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		swSandBodyRenderer.sharedMaterial = BodySandMats [0];
		swSandBodyRenderer.updateWhenOffscreen = true;
		swSandBodyRenderer.receiveShadows = false;

		/*Cloth cloth = swSandBodyRenderer.gameObject.AddComponent <Cloth> ();
		ClothSkinningCoefficient [] newConstraints = cloth.coefficients;
		for (int i = 0; i < newConstraints.Length; i++) {
			ClothSkinningCoefficient c = newConstraints [i];
			c.maxDistance = Random.value * 10f;
			c.collisionSphereDistance = 0f;
			newConstraints [i] = c;
		}
		cloth.coefficients = newConstraints;
		cloth.bendingStiffness = 0.61f;
		cloth.stretchingStiffness = 0.61f;
		cloth.worldVelocityScale = 600f;
		cloth.worldAccelerationScale = 1f;
		cloth.randomAcceleration = Vector3.zero;//RandomAcceleration;
		cloth.useGravity = true;
		cloth.useVirtualParticles = 1f;
		cloth.useContinuousCollision = 1f;
		cloth.collisionMassScale = 0.5f;
		cloth.sleepThreshold = 1f;
		cloth.damping = 1f;*/

		UnderSprayParent = GameObject.Find ("MouthCenter").transform;//GameObject.Find ("Head").transform;

		Spray = GameObject.Instantiate (SprayPrefab) as GameObject;
		Spray.transform.parent = GameObject.Find ("Mouth1_2").transform;
		Spray.transform.localPosition = Vector3.zero;
	}

	void OnDisable () {
		for (int i = 0; i < TailPieces.Length; i++) {
			TailPieces [i].localPosition = startPositions [i];
			TailPieces [i].localEulerAngles = startRotations [i];
		}
		if (Spray != null) {
			if (!Application.isPlaying) {
				GameObject.DestroyImmediate (Spray);
			}
		}
	}

	RaycastHit hit;

	void Update () {
		
		if (Application.isPlaying) {
			NormalizedPosition = Mathf.Clamp01 (((Time.time - InitialDelay) - startTime) / TotalTime);
		} else if (Suspend) {
			return;
		}


		if (Physics.Raycast (UnderSprayParent.position + Vector3.up * 100, Vector3.down, out hit, 1000)) {
			Vector3 rotation = UnderSprayParent.transform.eulerAngles;
			rotation.x = 0f;
			rotation.z = 0f;
			UnderSpray.transform.eulerAngles = rotation;
			UnderSpray.transform.position = hit.point;
			UnderSpray.transform.Translate (UnderSprayPositionOffset);
			UnderSpray.transform.Rotate (UnderSprayRotationOffset);
		}

		for (int i = 0; i < ParticleCurves.Length; i++) {
			float emission = ParticleCurves [i].Evaluate (NormalizedPosition);
			ParticleSystem.EmissionModule emit = ParticleSystems [i].emission;
			if (emission <= 0) {
				emit.enabled = false;
			} else {
				emit.enabled = true;
				ParticleSystem.MinMaxCurve rate = emit.rate;
				rate.constant = emission * ParticleEmissionRates [i];
				emit.rate = rate;
			}
		}

		Vector3 splinePos = SplineNodeParent.position;
		splinePos.y = WormHeightCurve.Evaluate (NormalizedPosition);
		SplineNodeParent.position = splinePos;

		Vector3 miniSprayPos = MiniSpray.transform.position;
		miniSprayPos.y = WormHeightCurve.Evaluate (NormalizedPosition);
		MiniSpray.transform.position = miniSprayPos;

		MiniSpray.throttle = MiniSprayCurve.Evaluate (NormalizedPosition);

		InitialSpray.throttle = InitialSprayCurve.Evaluate (NormalizedPosition);
		Vector3 initialSprayPos = InitialSpray.transform.position;
		initialSprayPos.y = InitialSprayYPos.Evaluate (NormalizedPosition);
		InitialSpray.transform.position = initialSprayPos;

		for (int i = 0; i < DuneBlendShapes.Length; i++) {
			Dunes.SetBlendShapeWeight (i, DuneBlendShapes [i].Evaluate (NormalizedPosition) * 100);
		}

		//update the spline (since we're animating nodes)
		for (int i = 0; i < SplineNodes.Length; i++) {
			Spline.SetControlPoint (i, SplineNodes [i].localPosition);
		}

		RumbleSource.volume = RumbleCurve.Evaluate (NormalizedPosition);

		for (int i = 0; i < BodySandMats.Length; i++) {
			BodySandMats[i].mainTextureOffset = MainTextureOffset [i] * NormalizedPosition;
			BodySandMats[i].SetTextureOffset ("_DetailAlbedoMap", DetailTextureOffset [i] * NormalizedPosition);
		}

		Worm.position = Spline.GetAdjustedPoint (NormalizedPosition + baseStartParam);
		Worm.LookAt (Worm.position + Spline.GetAdjustedDirection (NormalizedPosition + baseStartParam));
		Worm.Rotate (RotationOffset);
		for (int i = TailPieces.Length - 1; i >= 0; i--) {
			TailPieces [i].parent = null;
		}
		for (int i = TailPieces.Length - 1; i >= 0; i--) {
			TailPieces [i].position = Spline.GetAdjustedPoint (NormalizedPosition + startParams [i]);
			TailPieces [i].LookAt (TailPieces [i].position + Spline.GetAdjustedDirection (NormalizedPosition + startParams [i]));
			TailPieces [i].Rotate (RotationOffset);
		}
		for (int i = 0; i < TailPieces.Length; i++) {
			TailPieces [i].parent = startParents [i];
			TailPieces [i].localScale = Vector3.one;
		}

		animation ["SandwormAnimation"].normalizedTime = NormalizedPosition * WormAnimationMultiplier;
		splineAnimation ["SplineAnimation"].normalizedTime = NormalizedPosition;
		animation.Sample ();
		splineAnimation.Sample ();

		ChurnedSandMat.SetTextureOffset ("_SandTex", new Vector2 (FallingSandCurve.Evaluate (NormalizedPosition) * -10, 0));
		ChurnedSandMat.SetFloat ("_MaskTime", ChurnedSandCurve.Evaluate (NormalizedPosition));
		ChurnedSandMat.SetFloat ("_TimeOffset", ChurnedSandCurve.Evaluate (NormalizedPosition));
		ChurnedSandMat.SetVector ("_PosOffset", new Vector4 (0f, ChurnedSandCurve.Evaluate (NormalizedPosition), 0f, 0f));
		//ChurnedSandMat.SetFloat ("_Cutoff", CurnedSandCurve.Evaluate (NormalizedPosition));
	}

	void OnDrawGizmos () {
		if (Suspend)
			return;

		for (int i = 0; i < ParticleSystems.Length; i++) {
			if (ParticleSystems [i].emission.enabled) {
				Gizmos.color = Color.Lerp (Color.blue, Color.clear, 1f - ParticleCurves [i].Evaluate (NormalizedPosition));
				Gizmos.DrawCube (ParticleSystems [i].transform.position, ParticleSystems [i].shape.radius * Vector3.one);
			}
		}

		Gizmos.color = Color.Lerp (Color.blue, Color.clear, 1f - MiniSprayCurve.Evaluate (NormalizedPosition));
		Gizmos.DrawCube (MiniSpray.transform.position, Vector3.one);
		
		/*for (int i = 0; i < SplineNodes.Length; i++) {
			if (i > 0 && (SplineNodes [i] == SplineNodes [i - 1] || SplineNodes [i].position == SplineNodes [i - 1].position)) {
				Gizmos.color = Color.yellow;
				Gizmos.DrawSphere (SplineNodes [i].position, 25f);
			} else {
				Gizmos.color = Color.Lerp (Color.Lerp (Color.red, Color.clear, 0.9f), Color.Lerp (Color.blue, Color.clear, 0.9f), (float)i / SplineNodes.Length);
				Gizmos.DrawSphere (SplineNodes [i].position, 15f);
				if (i < SplineNodes.Length - 1) {
					Gizmos.DrawLine (SplineNodes [i].position, SplineNodes [i + 1].position);
				}
			}
		}*/
	}
}
