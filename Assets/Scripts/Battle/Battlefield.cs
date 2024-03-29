﻿using UnityEngine;

namespace Battle {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Camera;

    using DG.Tweening;

    using Enemy;

    using Player;

    using Random = Random;

    public class Battlefield : MonoBehaviour
    {
        public FieldTile FieldTilePrefab;
        public StaticCameraMarker CameraMarkerPrefab;
        public BattlePlayer BattlePlayerPrefab;
        public CursorController CursorPrefab;
        public DamageIndicator DamageIndicatorPrefab;

        public int fieldWidth = 8;
        public int fieldHeight = 8;
        public int enemySpawnWidth = 2;
        public int playerSpawnWidth = 2;
        
        public FieldTile[] Tiles;

        protected List<FieldTile> EnemySpawnTiles;

        [HideInInspector]
        public List<Enemy> Enemies;

        public List<FieldTile> PlayerWalkable;
        public List<FieldTile> PlayerAttackable;
        public Queue<Enemy> EnemiesToMove = new Queue<Enemy>();
        public bool EnemyCanMove = false;
        public int EnemyMovedCounter = 0;

        public Queue<Enemy> EnemiesToAttack = new Queue<Enemy>();
        public bool EnemyCanAttack = false;
        public int EnemyAttackedCounter = 0;
        
        private void Awake()
        {
            GameManager.Instance.Battlefield = this;

            this.Enemies = new List<Enemy>();
            this.Tiles = new FieldTile[this.fieldWidth * this.fieldHeight];
            
            this.GenerateField();
            foreach (var tile in this.Tiles)
            {
                tile.Reset();
            }
            
            this.EnemySpawnTiles = this.Tiles.Skip((this.fieldWidth - this.enemySpawnWidth) * this.fieldHeight).ToList();
            
            this.SpawnPlayer();
            this.SpawnEnemies();
            
            this.UpdateBattle();
        }

        private void GenerateField()
        {
            //Tiles
            var counter = 0;
            for (var i = 0; i < this.fieldWidth; ++i)
            {
                for (var j = 0; j < this.fieldHeight; ++j, ++counter)
                {
                    this.Tiles[counter] = Instantiate(this.FieldTilePrefab, this.transform);
                    this.Tiles[counter].transform.localPosition = new Vector3(i, j);
                }
            }
            
            //Connect them up!

            //right-left
            for (var i = 0; i < this.fieldWidth * (this.fieldHeight - 1); ++i)
            {
                this.Tiles[i].SetNeighbor(Direction.Right, this.Tiles[i + this.fieldWidth]);
            }

            for (var i = 0; i < this.fieldWidth * this.fieldHeight; ++i)
            {
                if ((i + 1) % this.fieldHeight == 0)
                    continue;

                this.Tiles[i].SetNeighbor(Direction.Up, this.Tiles[i + 1]);
            }

            //Camera Marker
            Instantiate(this.CameraMarkerPrefab, new Vector3((this.fieldWidth - 1) / 2f, (this.fieldHeight - 1) / 2f, -10), Quaternion.identity,
                        this.transform);
        }

        private void SpawnEnemies()
        {
            if (!GameManager.Instance.HasPredifinedEnemy)
            {
                GameManager.Instance.LevelData.EnemyPool.GetRandom().Spawn(this);   
            }
            else
            {
                for (var i = 0; i < GameManager.Instance.PredefinedEnemyCount; ++i)
                {
                    GameManager.Instance.PredefinedEnemy.Spawn(this);
                }
            }
            
            //this.UpdateTilesEnemyAttack();
        }

        private void SpawnPlayer()
        {
            var value = Random.Range(0, this.fieldHeight * this.playerSpawnWidth);
            var pos = this.Tiles[value].transform.position - new Vector3(0, 0, 0.1f);

            var player = Instantiate(this.BattlePlayerPrefab, pos, Quaternion.identity, this.transform);
            player.PositionTile = this.Tiles[value];
            
            //this.UpdateTilesPlayerWalk();
        }
        
        public bool CanSpawnEnemy() => this.EnemySpawnTiles.Count > 0;

        public Vector3 GetNewEnemyLocation()
        {
            var value = Random.Range(0, this.EnemySpawnTiles.Count);

            var tile = this.EnemySpawnTiles[value];
            this.EnemySpawnTiles.RemoveAt(value);
            
            return tile.transform.position - new Vector3(0, 0, 0.1f);
        }

        public void RegisterEnemy(Enemy enemy)
        {
            this.Enemies.Add(enemy);
        }

        public void UpdateTilesPlayerWalk()
        {
            /*
            foreach (var tile in this.Tiles) tile.Reset();
            
            var player = GameManager.Instance.BattlePlayer;
            var playerPos = player.transform.position;
            
            var walkableTiles = new List<FieldTile>();
            this.Tiles[this.fieldHeight * (int)playerPos.x + (int)playerPos.y].PlayerWalk(GameManager.Instance.stats
            .Movement + 1, 
            walkableTiles, new List<FieldTile>());

            this.PlayerWalkable = walkableTiles;
            */

            foreach (var tile in this.Tiles)
            {
                tile.Reset();
            }

            var player = GameManager.Instance.BattlePlayer;

            var walkableTiles = new List<FieldTile>();
            var playerTile = player.PositionTile;

            var checkQueue = new Queue<FieldTile>();
            checkQueue.Enqueue(playerTile);

            var checkedTiles = new List<FieldTile>();

            while (checkQueue.Count > 0)
            {
                var tile = checkQueue.Dequeue();

                if (checkedTiles.Contains(tile))
                    continue;

                checkedTiles.Add(tile);

                if (tile.HasEnemy)
                    continue;

                var distance = Mathf.Abs(playerTile.transform.position.x - tile.transform.position.x) 
                               + Mathf.Abs(playerTile.transform.position.y - tile.transform.position.y);

                if (distance <= GameManager.Instance.stats.Movement)
                {
                    tile.TileStatus |= TileStatus.Walkable;
                    walkableTiles.Add(tile);
                }

                if (distance < GameManager.Instance.stats.Movement)
                {
                    if (tile.UpNeighbor != null) checkQueue.Enqueue(tile.UpNeighbor);
                    if (tile.DownNeighbor != null) checkQueue.Enqueue(tile.DownNeighbor);
                    if (tile.LeftNeighbor != null) checkQueue.Enqueue(tile.LeftNeighbor);
                    if (tile.RightNeighbor != null) checkQueue.Enqueue(tile.RightNeighbor);
                }
            }

            this.PlayerWalkable = walkableTiles;
        }

        public void UpdateTilesPlayerAttack()
        {
            foreach (var tile in this.Tiles)
            {
                tile.ResetPlayerWalk();
            }

            var attackable = new List<FieldTile>();

            
            var playerTile = GameManager.Instance.BattlePlayer.PositionTile;
            
            attackable = this.Tiles.Where(t =>
                                          {
                                              var dist = Mathf.Abs(t.transform.position.x - playerTile.transform
                                                                                                      .position.x) +
                                                         Mathf.Abs(t.transform.position.y - playerTile.transform
                                                                                                      .position.y);

                                              return dist <= GameManager.Instance.stats.MaxDistance;
                                              
                                          }).ToList();

            try { 
                attackable.Remove(playerTile);}
            catch (Exception e)
            {
                // ignored
            }

            foreach (var fieldTile in attackable)
            {
                fieldTile.TileStatus |= TileStatus.PlayerAttackable;
            }

            //var checkqueue = new Queue<FieldTile>();
                
                /*checkqueue.Enqueue(playerTile);
    
                if (playerTile.HasEnemy)
                {
                    this.Enemies.Find(e => e.PositionTile = playerTile).Kill();
                }
    
                var checkedTiles = new List<FieldTile>();
                
                while (checkqueue.Count > 0)
                {
                    var tile = checkqueue.Dequeue();
    
                    if (checkedTiles.Contains(tile))
                        continue;
                    
                    var distance = Mathf.Abs(playerTile.transform.position.x - tile.transform.position.x) 
                                   + Mathf.Abs(playerTile.transform.position.y - tile.transform.position.y);
    
                    if (distance > GameManager.Instance.stats.MinDistance && distance <= GameManager.Instance.stats.MaxDistance)
                    {
                        tile.TileStatus |= TileStatus.PlayerAttackable;
                        attackable.Add(tile);
                    }
    
                    if (distance < GameManager.Instance.stats.MaxDistance)
                    {
                        if (tile.UpNeighbor != null) checkqueue.Enqueue(tile.UpNeighbor);
                        if (tile.DownNeighbor != null) checkqueue.Enqueue(tile.DownNeighbor);
                        if (tile.LeftNeighbor != null) checkqueue.Enqueue(tile.LeftNeighbor);
                        if (tile.RightNeighbor != null) checkqueue.Enqueue(tile.RightNeighbor);
    
                    }
                }*/
            
            this.PlayerAttackable = attackable;
            GameManager.Instance.Cursor.PlayerAttackable = attackable;
        }

        private void Update()
        {
            this.UpdateBattle();

            if (this.Enemies.Count == 0)
            {
                var sq = DOTween.Sequence();
                sq.AppendInterval(1f)
                  .OnComplete(() => { GameManager.Instance.EndBattle(); });
            }
        }

        public void UpdateBattle()
        {
            switch (GameManager.Instance.BattleState)
            {
                case BattleState.PlayerToMove:
                    if (GameManager.Instance.Cursor == null)
                    {
                        GameManager.Instance.Cursor = Instantiate(this.CursorPrefab, GameManager.Instance
                                                                                                .BattlePlayer.transform
                                                                                                .position -
                                                                                     new Vector3(0, 0, 0.1f),
                                                                  Quaternion.identity, this.transform);
                        this.UpdateTilesPlayerWalk();
                        
                        GameManager.Instance.Cursor.Moveable      = true;
                        GameManager.Instance.Cursor.PlayerMovable = this.PlayerWalkable;
                    }
                    break;
                case BattleState.PlayerMoving:
                    break;
                case BattleState.PlayerToAttack:
                    break;
                case BattleState.PlayerAttacking:
                    break;
                case BattleState.EnemiesMoving:
                    this.MoveEnemies();
                    break;
                case BattleState.EnemiesAttacking:
                    this.AttackOfEnemies();
                    break;
                case BattleState.EndAnimation:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void MoveEnemies()
        {
            if (!this.EnemyCanMove)
                return;

            if (this.EnemyMovedCounter >= this.Enemies.Count)
            {
                this.EnemyMovedCounter = 0;
                this.EnemiesToMove.Clear();
                this.EnemyCanMove = false;
                GameManager.Instance.BattleState = BattleState.EnemiesAttacking;
                this.EnemyCanAttack = true;
                return;
            }
            
            if (this.EnemiesToMove.Count == 0)
                foreach (var enemy in this.Enemies)
                    this.EnemiesToMove.Enqueue(enemy);

            var moving = this.EnemiesToMove.Dequeue();
            this.EnemyCanMove = !moving.Move();
            this.EnemyMovedCounter++;
        }

        public void AttackOfEnemies()
        {
            if (!this.EnemyCanAttack)
                return;

            if (this.EnemyAttackedCounter >= this.Enemies.Count)
            {
                this.EnemyAttackedCounter = 0;
                this.EnemiesToAttack.Clear();
                this.EnemyCanAttack = false;
                GameManager.Instance.BattleState = BattleState.PlayerToMove;
                return;
            }

            if (this.EnemiesToMove.Count == 0)
                foreach (var enemy in this.Enemies)
                    this.EnemiesToAttack.Enqueue(enemy);

            var attacking = this.EnemiesToAttack.Dequeue();
            this.EnemyCanAttack = !attacking.Attack();
            this.EnemyAttackedCounter++;
        }
    }
}
