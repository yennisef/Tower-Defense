using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // Fungsi Singleton
    private static LevelManager _instance = null;
    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelManager> ();
            }

            return _instance;
        }
    }

    [SerializeField] private int _maxLives = 3;
    [SerializeField] private int _totalEnemy = 15;
    [SerializeField] private Transform _turretUIParent;
    [SerializeField] private GameObject _turretUIPrefab;
    [SerializeField] private Turret[] _turretPrefabs;
    [SerializeField] private Enemy[] _enemyPrefabs;
    [SerializeField] private Transform[] _enemyPaths;
    [SerializeField] private float _spawnDelay = 5f;
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _statusInfo;
    [SerializeField] private Text _livesInfo;
    [SerializeField] private Text _totalEnemyInfo;

    private List<Turret> _spawnedTurrets = new List<Turret> ();
    private List<Enemy> _spawnedEnemies = new List<Enemy> ();
    private List<Bullet> _spawnedBullets = new List<Bullet> ();

    private int _currentLives;
    private int _enemyCounter;
    private float _runningSpawnDelay;

    public bool IsOver { get; private set; }

    private void Start ()
    {
        SetCurrentLives (_maxLives);
        SetTotalEnemy (_totalEnemy);
        InstantiateAllTurretUI ();
    }

    private void Update ()
    {
        // Jika menekan tombol R, fungsi restart akan terpanggil
        if (Input.GetKeyDown (KeyCode.R))
        {
            SceneManager.LoadScene (SceneManager.GetActiveScene ().name);
        }
        if (IsOver)
        {
            return;
        }
        
        // Counter untuk spawn enemy dalam jeda waktu yang ditentukan
        // Time.unscaledDeltaTime adalah deltaTime yang independent, tidak terpengaruh oleh apapun kecuali game object itu sendiri,
        // jadi bisa digunakan sebagai penghitung waktu
        _runningSpawnDelay -= Time.unscaledDeltaTime;
        if (_runningSpawnDelay <= 0f)
        {
            SpawnEnemy ();
            _runningSpawnDelay = _spawnDelay;
        }

        foreach (Turret turret in _spawnedTurrets)
        {
            turret.CheckNearestEnemy (_spawnedEnemies);
            turret.SeekTarget ();
            turret.ShootTarget ();
        }

        foreach (Enemy enemy in _spawnedEnemies)
        {
            if (!enemy.gameObject.activeSelf)
            {
                continue;
            }
            // Kenapa nilainya 0.1? Karena untuk lebih mentoleransi perbedaan posisi,
            // akan terlalu sulit jika perbedaan posisinya harus 0 atau sama persis
            if (Vector2.Distance (enemy.transform.position, enemy.TargetPosition) < 0.1f)
            {
                enemy.SetCurrentPathIndex (enemy.CurrentPathIndex + 1);
                if (enemy.CurrentPathIndex < _enemyPaths.Length)
                {
                    enemy.SetTargetPosition (_enemyPaths[enemy.CurrentPathIndex].position);
                }
                else
                {
                    ReduceLives(1);
                    enemy.gameObject.SetActive (false);
                }
            }
            else
            {
                enemy.MoveToTarget ();
            }
        }
    }

    // Menampilkan seluruh Turret yang tersedia pada UI Turret Selection
    private void InstantiateAllTurretUI ()
    {
        foreach (Turret turret in _turretPrefabs)
        {
            GameObject newTurretUIObj = Instantiate (_turretUIPrefab.gameObject, _turretUIParent);
            TurretUI newTurretUI = newTurretUIObj.GetComponent<TurretUI> ();
            newTurretUI.SetTurretPrefab (turret);
            newTurretUI.transform.name = turret.name;
        }
    }

    // Mendaftarkan Turret yang di-spawn agar bisa dikontrol oleh LevelManager
    public void RegisterSpawnedTurret (Turret turret)
    {
        _spawnedTurrets.Add (turret);
    }

    private void SpawnEnemy ()
    {
        SetTotalEnemy (--_enemyCounter);
        if (_enemyCounter < 0)
        {
            bool isAllEnemyDestroyed = _spawnedEnemies.Find (e => e.gameObject.activeSelf) == null;
            if (isAllEnemyDestroyed)
            {
                SetGameOver (true);
            }
            return;
        }
        int randomIndex = Random.Range (0, _enemyPrefabs.Length);
        string enemyIndexString = (randomIndex + 1).ToString ();
        GameObject newEnemyObj = _spawnedEnemies.Find (
            e => !e.gameObject.activeSelf && e.name.Contains (enemyIndexString)
        )?.gameObject;

        if (newEnemyObj == null)
        {
            newEnemyObj = Instantiate (_enemyPrefabs[randomIndex].gameObject);
        }
        Enemy newEnemy = newEnemyObj.GetComponent<Enemy> ();
        if (!_spawnedEnemies.Contains (newEnemy))
        {
            _spawnedEnemies.Add (newEnemy);
        }

        newEnemy.transform.position = _enemyPaths[0].position;
        newEnemy.SetTargetPosition (_enemyPaths[1].position);
        newEnemy.SetCurrentPathIndex (1);
        newEnemy.gameObject.SetActive (true);
    }

    public Bullet GetBulletFromPool (Bullet prefab)
    {
        GameObject newBulletObj = _spawnedBullets.Find (
            b => !b.gameObject.activeSelf && b.name.Contains (prefab.name)
        )?.gameObject;

        if (newBulletObj == null)
        {
            newBulletObj = Instantiate (prefab.gameObject);
        }
        Bullet newBullet = newBulletObj.GetComponent<Bullet> ();
        if (!_spawnedBullets.Contains (newBullet))
        {
            _spawnedBullets.Add (newBullet);
        }
        return newBullet;
    }

    public void ExplodeAt (Vector2 point, float radius, int damage)
    {
        foreach (Enemy enemy in _spawnedEnemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                if (Vector2.Distance (enemy.transform.position, point) <= radius)
                {
                    enemy.ReduceEnemyHealth (damage);
                }
            }
        }
    }

    public void ReduceLives (int value)
    {
        SetCurrentLives (_currentLives - value);
        if (_currentLives <= 0)
        {
            SetGameOver (false);
        }
    }

    public void SetCurrentLives (int currentLives)
    {
        // Mathf.Max fungsi nya adalah mengambil angka terbesar
        // sehingga _currentLives di sini tidak akan lebih kecil dari 0
        _currentLives = Mathf.Max (currentLives, 0);
        _livesInfo.text = $"Lives: {_currentLives}";
    }

    public void SetTotalEnemy (int totalEnemy)
    {
        _enemyCounter = totalEnemy;
        _totalEnemyInfo.text = $"Total Enemy: {Mathf.Max (_enemyCounter, 0)}";
    }

    public void SetGameOver (bool isWin)
    {
        IsOver = true;
        _statusInfo.text = isWin ? "You Win!" : "You Lose!";
        _panel.gameObject.SetActive(true);
    }

    // Untuk menampilkan garis penghubung dalam window Scene
    // tanpa harus di-Play terlebih dahulu
    private void OnDrawGizmos ()
    {
        for (int i = 0; i < _enemyPaths.Length - 1; i++)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine (_enemyPaths[i].position, _enemyPaths[i + 1].position);
        }
    }
}
