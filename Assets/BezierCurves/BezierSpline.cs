using UnityEngine;
using System;

//this is a combination of catlike coding & whydoidoit's curves & splines classes
//i created it to replace the SuperSplines library so this project could be open source
//i don't recommend using it outside this project
public class BezierSpline : MonoBehaviour {

	[SerializeField]
	private Vector3[] points;

	[SerializeField]
	private BezierControlPointMode[] modes;

	[SerializeField]
	private bool loop;

	public AnimationCurve AdjustedParam;
	public AnimationCurve AdjustedXRot;
	public AnimationCurve AdjustedYRot;
	public AnimationCurve AdjustedZRot;

	public bool Loop {
		get {
			return loop;
		}
		set {
			loop = value;
			if (value == true) {
				modes[modes.Length - 1] = modes[0];
				SetControlPoint(0, points[0]);
			}
		}
	}

	public int ControlPointCount {
		get {
			return points.Length;
		}
	}

	public Vector3 GetControlPoint (int index) {
		return points[index];
	}

	public void SetControlPoint (int index, Vector3 point) {
		if (index % 3 == 0) {
			Vector3 delta = point - points[index];
			if (loop) {
				if (index == 0) {
					points[1] += delta;
					points[points.Length - 2] += delta;
					points[points.Length - 1] = point;
				}
				else if (index == points.Length - 1) {
					points[0] = point;
					points[1] += delta;
					points[index - 1] += delta;
				}
				else {
					points[index - 1] += delta;
					points[index + 1] += delta;
				}
			}
			else {
				if (index > 0) {
					points[index - 1] += delta;
				}
				if (index + 1 < points.Length) {
					points[index + 1] += delta;
				}
			}
		}
		points[index] = point;
		EnforceMode(index);
	}

	public BezierControlPointMode GetControlPointMode (int index) {
		return modes[(index + 1) / 3];
	}

	public void SetControlPointMode (int index, BezierControlPointMode mode) {
		int modeIndex = (index + 1) / 3;
		modes[modeIndex] = mode;
		if (loop) {
			if (modeIndex == 0) {
				modes[modes.Length - 1] = mode;
			}
			else if (modeIndex == modes.Length - 1) {
				modes[0] = mode;
			}
		}
		EnforceMode(index);
	}

	private void EnforceMode (int index) {
		int modeIndex = (index + 1) / 3;
		BezierControlPointMode mode = modes[modeIndex];
		if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Length - 1)) {
			return;
		}

		int middleIndex = modeIndex * 3;
		int fixedIndex, enforcedIndex;
		if (index <= middleIndex) {
			fixedIndex = middleIndex - 1;
			if (fixedIndex < 0) {
				fixedIndex = points.Length - 2;
			}
			enforcedIndex = middleIndex + 1;
			if (enforcedIndex >= points.Length) {
				enforcedIndex = 1;
			}
		}
		else {
			fixedIndex = middleIndex + 1;
			if (fixedIndex >= points.Length) {
				fixedIndex = 1;
			}
			enforcedIndex = middleIndex - 1;
			if (enforcedIndex < 0) {
				enforcedIndex = points.Length - 2;
			}
		}

		Vector3 middle = points[middleIndex];
		Vector3 enforcedTangent = middle - points[fixedIndex];
		if (mode == BezierControlPointMode.Aligned) {
			enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
		}
		points[enforcedIndex] = middle + enforcedTangent;
	}

	public int CurveCount {
		get {
			return (points.Length - 1) / 3;
		}
	}

	//respects total length of spline
	public Vector3 GetAdjustedPoint (float t) {
		return GetPoint (AdjustedParam.Evaluate (t));
	}

	public Vector3 GetPoint (float t) {
		int i;
		if (t >= 1f) {
			t = 1f;
			i = points.Length - 4;
		}
		else {
			t = Mathf.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		return transform.TransformPoint(Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
	}

	public Vector3 GetAdjustedVelocity (float t) {
		return GetVelocity (AdjustedParam.Evaluate (t));
	}

	public Vector3 GetVelocity (float t) {
		int i;
		if (t >= 1f) {
			t = 1f;
			i = points.Length - 4;
		}
		else {
			t = Mathf.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		return transform.TransformPoint(Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
	}

	public Vector3 GetAdjustedDirection (float t) {
		return new Vector3 (AdjustedXRot.Evaluate (t), AdjustedYRot.Evaluate (t), AdjustedZRot.Evaluate (t));
	}

	public Vector3 GetDirection (float t) {
		return GetVelocity(t).normalized;
	}

	public void AddCurve () {
		Vector3 point = points[points.Length - 1];
		Array.Resize(ref points, points.Length + 3);
		point.x += 1f;
		points[points.Length - 3] = point;
		point.x += 1f;
		points[points.Length - 2] = point;
		point.x += 1f;
		points[points.Length - 1] = point;

		Array.Resize(ref modes, modes.Length + 1);
		modes[modes.Length - 1] = modes[modes.Length - 2];
		EnforceMode(points.Length - 4);

		if (loop) {
			points[points.Length - 1] = points[0];
			modes[modes.Length - 1] = modes[0];
			EnforceMode(0);
		}
	}

	public void Reset () {
		points = new Vector3[] {
			new Vector3(1f, 0f, 0f),
			new Vector3(2f, 0f, 0f),
			new Vector3(3f, 0f, 0f),
			new Vector3(4f, 0f, 0f)
		};
		modes = new BezierControlPointMode[] {
			BezierControlPointMode.Free,
			BezierControlPointMode.Free
		};
	}

	//this is the only function that sets lengths, so not using this breaks the spline
	public void AddPoints (Vector3 [] newPoints) {
		points = newPoints;
		modes = new BezierControlPointMode [newPoints.Length / 3 + 1];
		for (int i = 0; i < modes.Length; i++) {
			modes [i] = BezierControlPointMode.Free;
		}

		AdjustedParam = new AnimationCurve ();
		AdjustedParam.AddKey (0f, 0f);
		AdjustedParam.AddKey (1f, 1f);

		AdjustedXRot = new AnimationCurve ();
		AdjustedYRot = new AnimationCurve ();
		AdjustedZRot = new AnimationCurve ();

		int samples = 10;
		float stepSize = (1.0f / (points.Length - 1)) / samples;
		float distanceTraveled = 0;
		float currentStep = stepSize;
		Vector3 sampleCurrent = points[0];
		Vector3 sampleLast = points[0];

		//get the total real-world distance of the spline
		float totalLength = 0f;
		while (currentStep < 1) {
			sampleCurrent = GetPoint(currentStep);
			totalLength += Vector3.Distance(sampleLast, sampleCurrent);
			currentStep += stepSize;
			sampleLast = sampleCurrent;
		}

		//reset
		currentStep = stepSize;
		sampleCurrent = points[0];
		sampleLast = points[0];
		distanceTraveled = 0f;

		//do the position samples
		while (currentStep < 1) {
			sampleCurrent = GetPoint(currentStep);
			distanceTraveled += Vector3.Distance(sampleLast, sampleCurrent);
			float factor = distanceTraveled / totalLength;
			AdjustedParam.AddKey(factor, currentStep);
			currentStep += stepSize;
			sampleLast = sampleCurrent;
		}

		//do the rotation samples
		samples = 8;
		stepSize = (1.0f / (points.Length - 1)) / samples;
		currentStep = stepSize;
		sampleCurrent = points[0];
		sampleLast = points[0];
		Vector3 normal = Vector3.forward;
		Vector3 adjustedNormal = normal;
		distanceTraveled = 0f;

		while (currentStep < 1) {
			sampleCurrent = GetPoint(currentStep);
			normal = (sampleCurrent - sampleLast).normalized;
			distanceTraveled += Vector3.Distance(sampleLast, sampleCurrent);
			float factor = distanceTraveled / totalLength;
			adjustedNormal = Vector3.Lerp (adjustedNormal, normal, 0.25f);
			AdjustedXRot.AddKey (factor, adjustedNormal.x);
			AdjustedYRot.AddKey (factor, adjustedNormal.y);
			AdjustedZRot.AddKey (factor, adjustedNormal.z);

			currentStep += stepSize;
			sampleLast = sampleCurrent;
		}
	}

	//expensive brute-force function, should only be called on startup
	public float GetAdjustedTimeAtPoint (Vector3 point, float minStep, float minRange, float maxRange, float tolerance) {
		float time = minRange;
		float result = time;
		float minDistanceSoFar = Mathf.Infinity;
		while (time <= maxRange) {
			Vector3 compare = GetAdjustedPoint (time);
			float distance = Vector3.Distance (point, compare);
			if (distance < minDistanceSoFar) {
				minDistanceSoFar = distance;
				result = time;
				if (distance < tolerance) {
					break;
				}
			}
			time += minStep;
		}
		return result;
	}
}