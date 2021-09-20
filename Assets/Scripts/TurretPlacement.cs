using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretPlacement : MonoBehaviour
{
    private Turret _placedTurret;

    // Fungsi yang terpanggil sekali ketika ada object Rigidbody yang menyentuh area collider
    private void OnTriggerEnter2D (Collider2D collision)
    {
        if (_placedTurret != null && _placedTurret.IsPlaced && !_placedTurret.gameObject.activeSelf)
        {
            _placedTurret = null;
        }

        if (_placedTurret == null)
        {
            Turret turret = collision.GetComponent<Turret>();
            if (turret != null)
            {
                turret.SetPlacePosition(transform.position);
                _placedTurret = turret;
            }
        }
    }

    // Kebalikan dari OnTriggerEnter2D, fungsi ini terpanggil sekali ketika object tersebut meninggalkan area collider
    private void OnTriggerExit2D (Collider2D collision)
    {
        if (_placedTurret == null) return;

        if (!_placedTurret.IsPlaced)
        {
            _placedTurret.SetPlacePosition(null);
            _placedTurret = null;
        }
    }
}
