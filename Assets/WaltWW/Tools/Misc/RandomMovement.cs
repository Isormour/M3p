using UnityEngine;
using System.Collections;

namespace WaltWW
{
	public class RandomMovement : MonoBehaviour {

		public float randomRange = 1.0f;
		public Vector2 timeStepRange;

		private float iter = 0.0f;
		private float targetTime = 0.0f;
		private Vector3 startPosition;
		private Vector3 lastPosition = Vector3.zero;
		private Vector3 targetVector;

		// Use this for initialization
		void Awake ()
		{
			startPosition = transform.localPosition;
		}
	
		// Update is called once per frame
		void Update ()
		{
			if( iter >= targetTime )
			{
				targetTime = Random.Range( timeStepRange.x, timeStepRange.y );
				lastPosition = targetVector;
				targetVector = new Vector3( Random.Range( -randomRange, randomRange ), Random.Range( -randomRange, randomRange ), Random.Range( -randomRange, randomRange ) );
				iter = 0f;
			}
			transform.localPosition = startPosition + ( Vector3.Lerp( lastPosition, targetVector, iter / targetTime ) );
			iter += Time.deltaTime;
		}
	}
}
