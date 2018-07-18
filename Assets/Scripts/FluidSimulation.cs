using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FluidSimulation : Singleton<FluidSimulation> {

	[Header("Main Properties")]
	public int maxParticles = 1500;

	[Range(0, 0.1f)]
	public float particleRadius = 0.07f; // used for collision
	public float gravity = 3f;
	public float timeScale = 3f;

	[Header("Fluid Properties")]
	[Range(0, 1)]
	public float sigma = 0.9f; // for high viscosity

	[Range(0, 1)]
	public float beta = 0.3f; // set to non-zero for less viscosity
	public bool enableDensityRelaxation = false;
	public float restDensity = 6.4f;
	public float stiffness = 0.0061f;
	public float nearStiffness = 0.625f;

	private ParticleSystem fluidSystem;
	private List<FluidEmitter> emitters;

	private ObjectPooler<RoiParticle> objectPooler;
	private List<RoiParticle> roiParticles;
	private ParticleSystem.Particle[] shurikenParticles;
	private Bounds2D simulationBounds;
	private SpatialHash spatialHash;
	private int numberOfParticles;
	private List<RoiParticle>[] _nearParticles;

	private void Start () {
		roiParticles = new List<RoiParticle>();
		simulationBounds = Utils.getBounds(Camera.main);
		spatialHash = new SpatialHash(simulationBounds, particleRadius);
		objectPooler = new ObjectPooler<RoiParticle>(maxParticles);

		setUpEmitters();
		setUpParticleSystem();
		setUpShurikenParticles();
		setUpNearParticlesArray();
	}

	public void AddEmitter(FluidEmitter emitter) {
		if (emitters != null)
			emitters.Add(emitter);
	}

	public void RemoveEmitter(FluidEmitter emitter) {
		if (emitters != null)
			emitters.Remove(emitter);
	}

	private void FixedUpdate () {
		float deltaTime = Time.fixedDeltaTime;

		removeOutOfBoundsParticles(); // remove out of bound particles
		spatialHash.updateGrid(roiParticles);

		// change speed of simulation
		deltaTime = deltaTime * timeScale;

		// update the number of particles
		numberOfParticles = roiParticles.Count;

		if (numberOfParticles > _nearParticles.Length) {
			setUpNearParticlesArray();
		}

		for (int i = 0; i < numberOfParticles; i++) {
			RoiParticle iParticle = roiParticles[i];
			if (!iParticle.isNew) {
				// apply prediction relaxation scheme
				// section 3.0 of the paper - "Particle-based Viscoelastic Fluid Simulation"
				iParticle.velocity = (iParticle.position - iParticle.originalPosition) / deltaTime;
			}
			iParticle.isNew = false;

			// apply gravity
			iParticle.velocity = new Vector2(iParticle.velocity.x, iParticle.velocity.y - this.gravity * deltaTime);
			spatialHash.calcNearByParticles(iParticle, ref _nearParticles[i]);

			applyViscosity(iParticle, deltaTime, _nearParticles[i]);
		}

		for (int i = 0; i < numberOfParticles; i++) {
			RoiParticle iParticle = roiParticles[i];
			// save the original particle position, then apply velocity.
			iParticle.originalPosition = iParticle.position;
			iParticle.position =  iParticle.position + iParticle.velocity * deltaTime;
		}

		if (enableDensityRelaxation)
			doubleDensityRelaxation(deltaTime, _nearParticles);
	}

	//-------------------------------------------------------------------------------------
	// REMOVING OUT OF BOUNDS PARTICLES
	private void removeOutOfBoundsParticles() {
		for (int i = 0; i < roiParticles.Count; i++) {
			if (simulationBounds.isPointOutside(roiParticles[i].position)) {
				objectPooler.Recycle(roiParticles[i]);
				roiParticles.RemoveAt(i);
				i--;
			}
		}
	}

	//-------------------------------------------------------------------------------------
	// APPLY VISCOSITY
	// section 5.3 of the paper - "Particle-based Viscoelastic Fluid Simulation"
	// Simon Clavet, Philippe Beaudoin, and Pierre Poulin
	// ------------------------------------------------------------------------------------
	// Some modifications are done which is inspired from,
	// https://github.com/Erkaman/gl-water2d
	private void applyViscosity(RoiParticle iParticle, float deltaTime, List<RoiParticle> nearParticles) {
		for (int j = 0; j < nearParticles.Count; j++ ) {
			RoiParticle jParticle = nearParticles[j];

			Vector2 dp = iParticle.position - jParticle.position;
			float r = dp.magnitude;
			Vector2 dp_norm = dp / r;

			if (r <= 0 || r > particleRadius)
				continue;

			float u = Vector2.Dot(iParticle.velocity - jParticle.velocity, dp_norm);
			float I_div2_mag = 0;
			if (u > 0) {
				I_div2_mag = deltaTime * (1 - (r / particleRadius)) * (sigma * u + beta * u * u) * 0.5f;
				if (I_div2_mag > u) I_div2_mag = u;
			} else {
				I_div2_mag = deltaTime * (1 - (r / particleRadius)) * (sigma * u - beta * u * u) * 0.5f;
				if (I_div2_mag < u) I_div2_mag = u;
			}
			iParticle.velocity = iParticle.velocity - I_div2_mag * dp_norm;
			jParticle.velocity = jParticle.velocity + I_div2_mag * dp_norm;
		}
	}

	//-------------------------------------------------------------------------------------
	// APPLY DOUBLE DENSITY RELAXATION
	// section 4 of the paper - "Particle-based Viscoelastic Fluid Simulation"
	// Simon Clavet, Philippe Beaudoin, and Pierre Poulin
	// ------------------------------------------------------------------------------------
	private void doubleDensityRelaxation(float deltaTime, List<RoiParticle>[] nearParticles) {
		for (int i = 0; i < numberOfParticles; i++) {
			RoiParticle iParticle = roiParticles[i];
			List<RoiParticle> iNearParticles = nearParticles[i];

			float density = 0;
			float nearDensity = 0;

			for (int j = 0; j < iNearParticles.Count; j++ ) {
				float r = (iParticle.position - iNearParticles[j].position).magnitude;
				if (r <= 0 || r > particleRadius)
					continue;
				
				float q = 1 - (r / particleRadius);
				density += q * q;
				nearDensity += q * q * q;
			}

			float pressure = stiffness * (density - restDensity);
			float nearPressure = nearStiffness * nearDensity;
			Vector2 iFinalPosition = Vector2.zero;

			for (int j = 0; j < iNearParticles.Count; j++ ) {
				Vector2 dp = (iParticle.position - iNearParticles[j].position);
				float r = dp.magnitude;
				if (r <= 0 || r > particleRadius)
					continue;
				
				float q = 1 - (r / particleRadius);
				float Dij = deltaTime * deltaTime * (pressure * q + nearPressure * q * q) * 0.5f;
				Vector2 finalDp = (Dij / r) * dp;

				iNearParticles[j].position +=  finalDp;
				iFinalPosition -= finalDp;
			}
			iParticle.position += iFinalPosition;
		}
	}

	private void Update() {
		// emit particles from emitters
		emitParticlesFromEmitters(Time.deltaTime);
		
		// render particles
		if (numberOfParticles > shurikenParticles.Length) {
			setUpShurikenParticles();
		}
		for (int i = 0; i < roiParticles.Count; i ++) {
			roiParticles[i].updateShurikenParticle(ref shurikenParticles[i]);
		}
		fluidSystem.SetParticles(shurikenParticles, roiParticles.Count);
	}

	private void emitParticlesFromEmitters(float deltaTime) {
		for (int i = 0; i < emitters.Count; i++) {
			if (roiParticles.Count < maxParticles) {
				emitters[i].emitParticles(deltaTime, roiParticles, objectPooler, maxParticles);				
			}
		}
	}

	private void setUpParticleSystem() {
		fluidSystem = GetComponent<ParticleSystem>();
		if (fluidSystem == null) {
			fluidSystem = gameObject.AddComponent<ParticleSystem>() as ParticleSystem;
		}
		// change simulation space to world
		var main = fluidSystem.main;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
	}

	private void setUpShurikenParticles() {
		shurikenParticles = new ParticleSystem.Particle[maxParticles];
		for (int i = 0; i < shurikenParticles.Length; i ++) {
			shurikenParticles[i] = new ParticleSystem.Particle();
		}
	}

	private void setUpNearParticlesArray() {
		_nearParticles = new List<RoiParticle>[maxParticles];
		for (int i = 0; i < _nearParticles.Length; i++) {
			_nearParticles[i] = new List<RoiParticle>();
		}
	}

	private void setUpEmitters() {
		emitters = new List<FluidEmitter>();
		emitters.AddRange(FindObjectsOfType<FluidEmitter>());
	}
}
