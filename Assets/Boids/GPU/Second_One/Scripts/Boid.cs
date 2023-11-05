using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour 
{
    [HideInInspector]
    public Vector3[] tab_Dir;

    [HideInInspector]
    public Vector3 velocity;

    [HideInInspector]
    public int nbTeammates;

    [HideInInspector]
    public Vector3 alignmentForce;

    [HideInInspector]
    public Vector3 cohesionForce;

    [HideInInspector]
    public Vector3 seperationForce;

    BoidParameters boidParam;

    public Type type;

    float scaredTimer = 0;
    Vector3 escapeDir = Vector3.zero;

    private GameObject player;

    public enum Type
    {
        Bar = 0,
        Clown = 1,
        ClownRouge = 2,
        ChirurgienBleu = 3,
        Shark = 4
    }

    //Creation des rayons de la sphere de la Fibonacci
    public void Awake()
    {
        tab_Dir = new Vector3[300];

        float grad = Mathf.PI * 2 * ((1 + Mathf.Sqrt(5)) / 2);

        for (int i = 0; i < tab_Dir.Length; i++)
        {
            float angle = Mathf.Acos(1 - 2 * ((float)i / tab_Dir.Length));
            float declin = grad * i;

            tab_Dir[i] = new Vector3(Mathf.Cos(declin) * Mathf.Sin(angle), Mathf.Sin(declin) * Mathf.Sin(angle), Mathf.Cos(angle));
        }

        player = GameObject.FindWithTag("Player");
    }

    //Initialisation du boid
    public void Init(BoidParameters param)
    { 
        this.boidParam = param;

        velocity = transform.forward * boidParam.avgSpeed;
    }

    //Mise a jour du boid
    public void new_Boid()
    {
        Vector3 sumForce = Vector3.zero;

        if (nbTeammates > 0)
        {
            //Force finale calculee par le compute shader (3 lois)
            sumForce += alignmentForce + cohesionForce + seperationForce;
        }


        //Si le poisson peut être effrayé
        if (boidParam.canBeScared)
        {
            bool collisionPlayer = Physics.SphereCast(transform.position, 5f, transform.forward, out RaycastHit hitPlayer, 1f, boidParam.scareLayer);
            
            if (scaredTimer > 0)
            {
                scaredTimer -= Time.deltaTime;
                //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
                escapeDir = player.transform.position - transform.position;

                //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
                Vector3 collisionForce = - escapeDir.normalized * boidParam.escapeSpeed * boidParam.weight;

                sumForce += collisionForce;
            }
            else if (collisionPlayer)
            {
                scaredTimer = Random.Range(0.2f, 2);
            }
        }

        
        //Si une collision est detectee en face du boid

        bool collision = Physics.SphereCast(transform.position, boidParam.sphereRad, transform.forward, out RaycastHit hit, boidParam.detectCollDst, boidParam.obstacleLayer);
        if (collision)
        {
            escapeDir = Vector3.zero;

            //Parcours de la liste des directions echappatoires
            for (int i = 0; i < tab_Dir.Length; i++)
            {
                Vector3 dir_gold = transform.TransformDirection(tab_Dir[i]);
                Ray ray = new Ray(transform.position, dir_gold);

                //Choisir cette direction si elle permet d'eviter l'obstacle
                bool canEscape = !Physics.SphereCast(ray, boidParam.sphereRad, out RaycastHit hit2, boidParam.detectCollDst, boidParam.obstacleLayer);
                if (canEscape)
                {
                    escapeDir = dir_gold;
                    break;
                }

                //Sinon continuer dans la direction actuelle
                else escapeDir = transform.forward;
            }

            //Mise a jour de la force finale avec la force d'evitement clamper a un maximum
            Vector3 collisionForce = Vector3.ClampMagnitude(escapeDir.normalized * boidParam.maxSpeed - velocity, boidParam.maxSteerForce) * boidParam.weight;

            sumForce += collisionForce;

        }

        if (Vector3.Distance(transform.position, player.transform.position) > 100)
        {
            escapeDir = player.transform.position - transform.position;

            //Mise a jour de la force finale avec une force rapprochant le poisson du joueur
            Vector3 collisionForce = escapeDir.normalized * boidParam.escapeSpeed * boidParam.weight;

            sumForce += collisionForce;
        }

        //Mise à jour du mouvement avec la force finale
        velocity += sumForce * Time.deltaTime;

        transform.forward = velocity / velocity.magnitude;
        if(scaredTimer > 0) velocity = transform.forward * boidParam.escapeSpeed;
        else velocity = transform.forward * Mathf.Clamp(velocity.magnitude, boidParam.minSpeed, boidParam.maxSpeed);

        transform.position += velocity * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y, 9.5f), transform.position.z);
    }
}