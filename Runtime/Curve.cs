using UnityEngine;

namespace Transient {
    public class CatmullRomCurveSegment {
		public float DxDyAverage { get; private set; }
		public float Length { get; private set; }
		public Vector3 P0;
		public Vector3 P1;
		public Vector3 P2;
		public Vector3 P3;
		float t0, t1, t2, t3;
		float t10, t21, t32;
		float t31, t20;
		public bool valid;

		public void SampleCurveLength(int count) {
			Length = 0f;
			DxDyAverage = 0;
			var start = P1;
            for (int i = 1; i <= count; i++) {
				var p = (float)i / count;
                var end = Sample(p);
                var l = (start - end).magnitude;
				Length += l;
				var dxdy = 1 / (l * count);
				DxDyAverage += dxdy;
				start = end;
			}
			DxDyAverage /= count;
		}

		public void Reset(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
			P0 = p0;
			P1 = p1;
			P2 = p2;
			P3 = p3;
			valid = true;
		}

		public void Append(Vector3 p) {
			P0 = P1;
			P1 = P2;
			P2 = P3;
			P3 = p;
		}

		public void Clear() {
			P0 = Vector3.zero;
			P1 = Vector3.zero;
			P2 = Vector3.zero;
			P3 = Vector3.zero;
			valid = false;
		}

		private float NextT(float t, Vector3 p, Vector3 n, float factor) {
			var d = n - p;
			return t + Mathf.Pow(Vector3.Dot(d, d), factor * 0.5f);
		}

		//factor controls the degree of bending
		public void Setup(float factor = 0.5f) {
			t0 = 0f;
			t1 = NextT(t0, P0, P1, factor);
			t2 = NextT(t1, P1, P2, factor);
			t3 = NextT(t2, P2, P3, factor);

			t10 = t1 == t0 ? 0 : 1 / (t1 - t0);
			t21 = t2 == t1 ? 0 : 1 / (t2 - t1);
			t32 = t3 == t2 ? 0 : 1 / (t3 - t2);

			t31 = t3 == t1 ? 0 : 1 / (t3 - t1);
			t20 = t2 == t0 ? 0 : 1 / (t2 - t0);
		}

		public Vector3 Sample(float t) {
			t = Mathf.Lerp(t1, t2, t);
			//fast path
			if (t == t1) return P1;
			if (t == t2) return P2;
			var P01 = Vector3.LerpUnclamped(P0, P1, (t - t0) * t10);
			var P12 = Vector3.LerpUnclamped(P1, P2, (t - t1) * t21);
			var P23 = Vector3.LerpUnclamped(P2, P3, (t - t2) * t32);
			var P012 = Vector3.LerpUnclamped(P01, P12, (t - t0) * t20);
			var P123 = Vector3.LerpUnclamped(P12, P23, (t - t1) * t31);
			var ret = Vector3.LerpUnclamped(P012, P123, (t - t1) * t21);
			return ret;
		}
	}
}