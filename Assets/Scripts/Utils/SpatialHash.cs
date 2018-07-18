using System.Collections.Generic;
using UnityEngine;


public class SpatialHash {

    private Dictionary<int, List<RoiParticle>> hashMap;

    private Bounds2D simBounds;
    private float cellSize;
    private float squaredSupportRadius;

    private int xCells;
    private int yCells;

    public SpatialHash(Bounds2D bounds, float cellSize) {
        this.simBounds = bounds;
        this.cellSize = cellSize;

        xCells = Mathf.RoundToInt((this.simBounds.max.x - this.simBounds.min.x) / cellSize);
        yCells = Mathf.RoundToInt((this.simBounds.max.y - this.simBounds.min.y) / cellSize);

        hashMap = new Dictionary<int, List<RoiParticle>>();
        squaredSupportRadius = cellSize * cellSize;
    }

    public void updateGrid(List<RoiParticle> particles) {
        // flush the map
        hashMap.Clear();

        // make the mappings again
        for (int i = 0; i < particles.Count; i++) {
            int key = toKey(toGridIndex(particles[i].position));
            if (hashMap.ContainsKey(key)) {
                hashMap[key].Add(particles[i]);
            } else {
                hashMap[key] = new List<RoiParticle> { particles[i] };
            }
        }
    }

    public void calcNearByParticles(RoiParticle particle, ref List<RoiParticle> nearParticles) {
        nearParticles.Clear();
        Vector2 gridIndex = toGridIndex(particle.position);
        // search for near particles in 9x9 grid
        for (int i = (int)gridIndex.x - 1; i <= gridIndex.x + 1; i++) {
            for (int j = (int)gridIndex.y - 1; j <= gridIndex.y + 1; j++) {
                // sanity check: we are not outside the simulation bounds
                if (i < 0 || j < 0 || i > xCells || j > yCells)
                    continue;
                
                int key = toKey(i, j);
                if (hashMap.ContainsKey(key)) {
                    List<RoiParticle> _paticles = hashMap[key];
                    for(var k = 0; k < _paticles.Count; k++) {
                        var d = (particle.position - _paticles[k].position).sqrMagnitude;
                        if (d < this.squaredSupportRadius && d > 0) {// if within support radius, add
                            nearParticles.Add(_paticles[k]);
                        }
                    }
                }
            }
        }
    }

    private Vector2 toGridIndex(Vector2 position) {
        return new Vector2(
            Mathf.FloorToInt((position.x - this.simBounds.min.x) / cellSize),
            Mathf.FloorToInt((position.y - this.simBounds.min.y) / cellSize)
        );
    }

    private int toKey(Vector2 gridIndex) {
        return (int)(gridIndex.x + gridIndex.y * xCells);
    }

    private int toKey(int x, int y) {
        return (x + y * xCells);
    }
}