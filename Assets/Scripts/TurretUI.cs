using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TurretUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _turretIcon;

    private Turret _turretPrefab;
    private Turret _currentSpawnedTurret;

    public void SetTurretPrefab (Turret turret)
    {
        _turretPrefab = turret;
        _turretIcon.sprite = turret.GetTurretHeadIcon ();
    }

    // Implementasi dari Interface IBeginDragHandler
    // Fungsi ini terpanggil sekali ketika pertama men-drag UI
    public void OnBeginDrag (PointerEventData eventData)
    {
        GameObject newTurretObj = Instantiate (_turretPrefab.gameObject);
        _currentSpawnedTurret = newTurretObj.GetComponent<Turret> ();
        _currentSpawnedTurret.ToggleOrderInLayer (true);
    }

    // Implementasi dari Interface IDragHandler
    // Fungsi ini terpanggil selama men-drag UI
    public void OnDrag (PointerEventData eventData)
    {
        Camera mainCamera = Camera.main;
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        Vector3 targetPosition = Camera.main.ScreenToWorldPoint (mousePosition);

        _currentSpawnedTurret.transform.position = targetPosition;
    }

    // Implementasi dari Interface IEndDragHandler
    // Fungsi ini terpanggil sekali ketika men-drop UI ini

    public void OnEndDrag (PointerEventData eventData)
    {
        if (_currentSpawnedTurret.PlacePosition == null)
        {
            Destroy (_currentSpawnedTurret.gameObject);
        }
        else
        {
            _currentSpawnedTurret.LockPlacement ();
            _currentSpawnedTurret.ToggleOrderInLayer (false);
            LevelManager.Instance.RegisterSpawnedTurret (_currentSpawnedTurret);
            _currentSpawnedTurret = null;
        }
    }
}
