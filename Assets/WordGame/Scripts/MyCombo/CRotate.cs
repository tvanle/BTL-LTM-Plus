using UnityEngine;
using System.Collections;

public class CRotate : MonoBehaviour {
    public float speed;

	private void Update()
    {
        this.transform.Rotate(Vector3.forward * Time.deltaTime * this.speed);
    }
}
