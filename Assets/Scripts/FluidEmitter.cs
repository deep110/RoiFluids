using System.Collections.Generic;
using UnityEngine;

public class FluidEmitter : MonoBehaviour {

	[Header("Fluid Properties")]
	public float particleSize = 0.35f;
	public Color fluidColor = new Color(0, 0, 1, 1);

	[Header("Emitter Properties")]
	public float frequency = 20f;
	public int angularVelocity = 0;
	public int emitAngle = 70;
	public float strength = 0.6f;
	public float velocityRandomness = 2f;

	private Transform _transform;
	private int currentAngle;
	private float timer;

	private void OnEnable () {
		_transform = GetComponent<Transform>();

		if (FluidSimulation.Instance)
			FluidSimulation.Instance.AddEmitter(this);
	}

	private void OnDisable () {
		_transform = null;

		if (FluidSimulation.Instance)
			FluidSimulation.Instance.RemoveEmitter(this);
	}

	public void emitParticles(float deltaTime, List<RoiParticle> particles,
			ObjectPooler<RoiParticle> objectPooler, int maxParticles) {
		// acumulate time till we exceed frequency
		timer = timer + deltaTime;
		float period = 1.0f / frequency;

		if (timer > period) {
			// get current emit angle
			if (angularVelocity == 0) {
                // if not angle velocity, just use emit angle
                currentAngle = emitAngle;
            } else {
                // if we have angle velocity, ignore emit angle, and rotate by angular velocity.
                currentAngle += angularVelocity;
            }

			// get base velocity and color of emitting particle
			Vector2 velocity = Utils.getVector2(strength, currentAngle);
			Vector2 perpendicularVelocity = new Vector2(-velocity.y, velocity.x).normalized;
			float a = velocityRandomness * 0.01f;

			for (var j = -1; j <= 1; j++) {
				Vector2 pos = (Vector2)_transform.position + 0.04f * j * perpendicularVelocity;

				if (particles.Count < maxParticles) {
					RoiParticle roiParticle =  objectPooler.GetObject();
					roiParticle.Update(
						pos,
						new Vector2(velocity.x + Random.Range(-a, a), velocity.y + Random.Range(-a, a)),
						particleSize,
						fluidColor
					);
					particles.Add(roiParticle);
				} else {
					break;
				}
            }

			// reset timer, instead of zero take difference
			timer = timer - period;
		}
	}
}
