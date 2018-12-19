using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum PlayerTeam
{
    None,
    BlueTeam,

    RedTeam
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _sharedSpawnPoints = new List<SpawnPoint>();
    System.Random _random = new System.Random();
    float _closestDistance;
    [Tooltip("This will be used to calculate the second filter where algorithm looks for closest friends, if the friends are away from this value, they will be ignored")]
    [SerializeField] private float _maxDistanceToClosestFriend = 30;
    [Tooltip("This will be used to calculate the first filter where algorithm looks for enemies that are far away from this value. Only enemies which are away from this value will be calculated.")]
    [SerializeField] private float _minDistanceToClosestEnemy = 10;
    [Tooltip("This value is to prevent friendly player spawning on top of eachothers. If a player is within the range of this value to a spawn point, that spawn point will be ignored")]
    [SerializeField] private float _minMemberDistance = 2;


    public DummyPlayer PlayerToBeSpawned;
    public DummyPlayer[] DummyPlayers;




    // Sahnedeki DummyPlayer ve SpawnPoin objeleri bulunur.
    private void Awake()
    {
		_sharedSpawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());

		DummyPlayers = FindObjectsOfType<DummyPlayer>();
    }


    // Oyuncunun takım arkadaşlarının, ve düşman oyuncaların mesafelerine bakarak herdefasında oyuncu için en ideal spawn noktalarını berirler ve ilklerinden seçer.

    #region SPAWN ALGORITHM
    public SpawnPoint GetSharedSpawnPoint(PlayerTeam team)
    {
        List<SpawnPoint> spawnPoints = new List<SpawnPoint>(_sharedSpawnPoints.Count);
        CalculateDistancesForSpawnPoints(team);
        GetSpawnPointsByDistanceSpawning(ref spawnPoints);
        if (spawnPoints.Count <= 0)
        {
            GetSpawnPointsBySquadSpawning(team, ref spawnPoints);
        }
        //SpawnPoint spawnPoint = spawnPoints.Count <= 1 ? spawnPoints[0] : spawnPoints[_random.Next(0, (int)((float)spawnPoints.Count * .5f))];
        //Her zaman sırayı takip etmesini istediğmiz için// squad spawn zaten tek bir nokta gonderiyor
        SpawnPoint spawnPoint = spawnPoints[0];
        spawnPoint.StartTimer();
        return spawnPoint;
    }

    // Daha önceki metodlarda aldığımız mesafe verilerini kurallarda bulunan verilerle karşılaştırarak spawn objesi listesine uygun spawn objelerini ekler.
    private void GetSpawnPointsByDistanceSpawning(ref List<SpawnPoint> suitableSpawnPoints)
    {


        //CalculateDistancesForSpawnPoints metodunda her spawn noktası için kaydettiğimiz en yakın düşman ve dost oyuncuların mesafelerini, gerekli mesafelerle karşılaştırarak uygun noktaları listeye ekler.
        //Aynı zamanda söz konusu spawn noktasında 2 saniye içerisinde bir spawn olup olmadığını kontrol eder.
        // Objelerin listeye giriş sırası unity sahnesinde ki sıradan farklı ve sizin istediğiniz sıranın tersinde olduğu için listeyi noktalar belli olduktan sonra ters çevirdim.
        _sharedSpawnPoints.Clear();
        _sharedSpawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());
        //Squad spawn, liste sırasını karıştırdığı için. 

        for (int i = 0; i < _sharedSpawnPoints.Count; i++) {           
            if (_sharedSpawnPoints[i].DistanceToClosestEnemy >= _minDistanceToClosestEnemy && _sharedSpawnPoints[i].DistanceToClosestFriend >= _minMemberDistance && _sharedSpawnPoints[i].SpawnTimer <= 0) {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
                Debug.Log(_sharedSpawnPoints[i] + " <color=green>eklendi</color> . En yakın dusman: <color=green>" + _sharedSpawnPoints[i].DistanceToClosestEnemy + "</color> En yakin arkadas: <color=green>" + _sharedSpawnPoints[i].DistanceToClosestFriend + "</color> Önceki spawn:<color=green>" + _sharedSpawnPoints[i].SpawnTimer + "</color>");
                _sharedSpawnPoints[i].GetComponent<SpawnPointGizmo>()._color = Color.green;
                
            } 
            
            
            
            //////******* DEBUG BAŞLANGIÇ
            else {
                _sharedSpawnPoints[i].GetComponent<SpawnPointGizmo>()._color = Color.red;

                if (_sharedSpawnPoints[i].DistanceToClosestEnemy < _minDistanceToClosestEnemy)
                    Debug.Log("<color=red>Yakın enemy var. </color>" + _sharedSpawnPoints[i].name);
                else if (_sharedSpawnPoints[i].DistanceToClosestFriend < _minMemberDistance)
                    Debug.Log("<color=red>Yakın friendly var. </color>" + _sharedSpawnPoints[i].name);
                else if (_sharedSpawnPoints[i].SpawnTimer > 0) { 
                    Debug.Log("<color=yellow>Yeni spawn var. </color>" + _sharedSpawnPoints[i].name);
                    _sharedSpawnPoints[i].GetComponent<SpawnPointGizmo>()._color = Color.yellow;
                }

            }

            //////******* DEBUG BİTİŞ
        }
        suitableSpawnPoints.Reverse();
    }

    private void GetSpawnPointsBySquadSpawning(PlayerTeam team, ref List<SpawnPoint> suitableSpawnPoints)
    {
        if (suitableSpawnPoints == null)
        {
            suitableSpawnPoints = new List<SpawnPoint>();
        }
        suitableSpawnPoints.Clear();
        _sharedSpawnPoints.Sort(delegate (SpawnPoint a, SpawnPoint b)
        {
            if (a.DistanceToClosestFriend == b.DistanceToClosestFriend)
            {
                return 0;
            }
            if (a.DistanceToClosestFriend > b.DistanceToClosestFriend)
            {
                return 1;
            }
            return -1;
        });
        for (int i = 0; i < _sharedSpawnPoints.Count && _sharedSpawnPoints[i].DistanceToClosestFriend <= _maxDistanceToClosestFriend; i++)
        {
            if (!(_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && !(_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance) && _sharedSpawnPoints[i].SpawnTimer <= 0)
            {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
            }
        }
        if (suitableSpawnPoints.Count <= 0)
        {
            suitableSpawnPoints.Add(_sharedSpawnPoints[0]);
        }

        

    }



    // Her spawn point için sırasıyla GetDistanceToClosestMember metodu içerisinde önce takım arkadaşı oyuncularının mesafelerini kontrol eder, sonrası da düşman takımın.
    private void CalculateDistancesForSpawnPoints(PlayerTeam playerTeam)
    {
        for (int i = 0; i < _sharedSpawnPoints.Count; i++)
        {
            _sharedSpawnPoints[i].DistanceToClosestFriend = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam);
            _sharedSpawnPoints[i].DistanceToClosestEnemy = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam == PlayerTeam.BlueTeam ? PlayerTeam.RedTeam : playerTeam == PlayerTeam.RedTeam ? PlayerTeam.BlueTeam : PlayerTeam.None);       
        }
    }


    // HATA - _closestdistance 0 başladığı için hiçbir zaman doğru değeri dönemiyor. Karşılaştıracak bir değer yok. Bir oyuncunun diğer oyuncudan daha yakın olup olmadığını bulmamıza engel oluyor.   
    // Kabul edilebilir en yüksek değeri verebiliriz. _minDistanceToClosestEnemy
    private float GetDistanceToClosestMember(Vector3 position, PlayerTeam playerTeam) {
        _closestDistance = 10;
        foreach (var player in DummyPlayers)
        {
           
            if (!player.Disabled && player.PlayerTeamValue != PlayerTeam.None && player.PlayerTeamValue == playerTeam && !player.IsDead())
            {
                float playerDistanceToSpawnPoint = Vector3.Distance(position, player.Transform.position);

               // Debug.Log(  player +"----"+ playerDistanceToSpawnPoint);
                if (playerDistanceToSpawnPoint < _closestDistance)
                {
                    _closestDistance = playerDistanceToSpawnPoint;

                }
              

            }
        }
      
        return _closestDistance;
    }

    #endregion
    /// <summary>
    /// Genel olarak, spawn vakti geldiği zaman her spawn noktası etrafındaki her oyuncunun kendisine olan uzaklığını kontrol eder.
    /// Her spawn noktası kendisine en yakın düşman ve dost oyuncunun uzaklığını kaydeder.
    /// Eğer bir oyuncunun uzaklığı spawn noktasına kendisi için(dost/dusman) verilen uzaklık limitlerinin içerisindeyse o spawn noktası uygun değildir.
    /// En yakın düşman, en yakın dost ve spawn gecikmesi kontrol edildiğinde şartlar uyuyorsa spawn noktası uygun spawnlar listesine eklenir.
    /// Liste içerisinde uygun spawn nokta sayısı ne kadar çok ise, belli spawnlara öncelik vererek arasında spawn noktası seçilir.
    /// 
    /// NOT: Verdiğiniz pdf te sırasıyla 1-2-3-4-5 noktalırında spawn olunması gerektiği yazıyor. Fakat algoritma uygun spawn noktasına birden fazla spawn noktası seçebiliyor.
    /// Algoritmayı her zaman ilk spawn noktasını seçmesi olarak değiştirdim.
    /// Sahnedeki spawn noktalarının listeye farklı sırada girmesi oldukça kafa karıştırmış. Üstelik squad spawn bu sırayı takım arkadaşlarına göre değiştiriyor.
    /// Normal spawn çağrıldığı durumunda listeyi temizleyip tekrar oluşturdum.
    ///    
    /// Kodun çalışmasını engelleyen hata ise _closestDistance değişkeninin bir karşılığı olmaması.
    /// </summary>
    /// 


    public void TestGetSpawnPoint()
    {
      
    	SpawnPoint spawnPoint = GetSharedSpawnPoint(PlayerToBeSpawned.PlayerTeamValue);
    	PlayerToBeSpawned.Transform.position = spawnPoint.PointTransform.position;
    }

    
}