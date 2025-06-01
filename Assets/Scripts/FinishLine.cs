using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        Player player = other.GetComponentInParent<Player>();
        if (player != null && Player.list.ContainsKey(player.Id))
        {
            RaceManager.Singleton.OnPlayerCrossedFinishLine(player);
        }
    }
}
