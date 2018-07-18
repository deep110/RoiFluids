using UnityEngine;

public class RoiParticle {
    public Vector2 position;
    public Vector2 velocity;

    public float radius;
    public Color color;

    public Vector2 originalPosition;
    public Vector2 finalPosition;
    public bool isNew;

    public RoiParticle() {}

    public RoiParticle(Vector2 position, Vector2 velocity, float radius, Color color) {
        Update(position, velocity, radius, color);
    }

    public void Update(Vector2 position, Vector2 velocity, float radius, Color color) {
        this.position = position;
        this.velocity = velocity;
        this.radius = radius;
        this.color = color;
        this.isNew = true;
    }

    public void updateShurikenParticle(ref ParticleSystem.Particle shurikenParticle) {
		shurikenParticle.startColor = this.color;
        shurikenParticle.startSize = this.radius;
        shurikenParticle.position = this.position;
    }

    public override string ToString() {
        return "position: " + this.position + "\n" + "velocity: " + this.velocity;
    }
}