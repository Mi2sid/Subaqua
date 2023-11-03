using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ce 'Singleton' permet d'avoir plusieurs réserves d'objets
/// Pour créer une nouvelle réserve utiliser 'CreateNewPool', la réserve est
/// identifée par l'identifiant retourné
/// </summary>
public class GameObjectPool : MonoBehaviour
{
    public static GameObjectPool inst;
    
    private Dictionary<int, Stack<GameObject>> pools;
    private Dictionary<int, GameObject> baseObjects;
    private int nextId = 0;
    
    private void Awake()
    {
        pools = new Dictionary<int, Stack<GameObject>>();
        baseObjects = new Dictionary<int, GameObject>();
        inst = this;
    }

    /// <summary>
    /// Crée et retourne une nouvelle réserve de 'baseObject', le montant initial de l'objet peut être spécifié
    /// </summary>
    public int CreateNewPool(GameObject baseObject, int baseAmount = 1)
    {
        Stack<GameObject> pool = new Stack<GameObject>(baseAmount);
        pools.Add(nextId, pool);
        baseObjects.Add(nextId, baseObject);
        
        Extend(nextId, baseAmount); // Ajout de 'baseAmount' objet
        
        return nextId++;
    }


    /// <summary>
    /// Demande 'amount' objets à la réserve identifée par 'id', si il n'y à pas assez d'objet
    /// étand la réserve.
    /// </summary>
    public GameObject[] Request(int id, int amount)
    {
        GameObject[] data = new GameObject[amount];
        Stack<GameObject> pool = pools[id];
        
        if (pool.Count < amount) // Si il n'y à pas assez d'objet
            Extend(id, amount);
        
        for (int i = 0; i < amount; i++)
            data[i] = pool.Pop();

        return data;
    }

    /// <summary>
    /// Libère les objets de la réserve 'id'
    /// </summary>
    public void Free(int id, GameObject[] objects)
    {
        Stack<GameObject> pool = pools[id];

        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(false);
            pool.Push(objects[i]);
        }
        
    }

    /// <summary>
    /// Crée de nouveau objets pour la réserve 'id'
    /// </summary>
    private void Extend(int id, int amount)
    {
        GameObject baseObject = baseObjects[id];
        Stack<GameObject> pool = pools[id];
        
        for (int i = 0; i < amount; i++)
        {
            GameObject tmp = Instantiate(baseObject);
            tmp.SetActive(false);
            pool.Push(tmp);
        }
    }
}
