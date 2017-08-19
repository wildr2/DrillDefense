using UnityEngine;
using System.Collections;

public class DisableOnPlay : MonoBehaviour
{
	private void Update()
    {
        gameObject.SetActive(false);
    }
}
