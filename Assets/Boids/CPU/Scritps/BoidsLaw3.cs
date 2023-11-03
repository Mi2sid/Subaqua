using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Boids))]
public class BoidsLaw3 : MonoBehaviour
{
    private Boids boid;

    public float radius;

    public float repulsionForce;
    void Start()
    {
        boid = GetComponent<Boids>();
    }

    // Update is called once per frame
    void Update()
    {
        var boids = FindObjectsOfType<Boids>();

        var average = Vector3.zero;
        var found = 0;

        foreach (var boid in boids.Where(b => b != boid))
        {
            var diff = boid.transform.position - this.transform.position;
            if (diff.magnitude < radius)
            {
                average += diff;
                found += 1;
            }
        }

        if (found > 0)
        {
            average = average / found;
            boid.velocity += Vector3.Lerp(boid.velocity, average, Time.deltaTime) * repulsionForce;
        }
    }
}
