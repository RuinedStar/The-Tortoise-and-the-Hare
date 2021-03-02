using System;
using System.Collections.Generic;
using System.Linq;
using THpuzzle;

namespace GameCore
{
    class Game
    {
        public enum MapKind { Space, Occupy, TtDest, HrDest };
        public enum PieceKind { Turtoise, Rare, Gaming };

        public class MapUnit
        {
            MapKind status;

            public MapKind Status { get { return status; } }
          
            public MapUnit(MapKind kind) { status = kind; }
            public void BeOccupied() { if( status == MapKind.Space ) status = MapKind.Occupy; }
            public void BeSpaced() { if ( status == MapKind.Occupy ) status = MapKind.Space; }
        }

        class Piece
        {
            readonly MapKind dest;
            readonly PieceKind kind;
            int loc;
            bool arrive;

            public PieceKind Kind { get { return kind; } }
            public MapKind destKind { get { return dest; } }
            public int Location { get { return loc; } set { loc = value; } }
            public bool Arrived { get { return arrive; } }
           
            public Piece(PieceKind k, int location) 
            {
                kind = k;
                if (k == PieceKind.Turtoise) dest = MapKind.TtDest;
                else dest = MapKind.HrDest;
                loc = location;
                arrive = false;
            }
            public void TestArrival(MapKind status) { arrive = ( (status == dest) ? true : false ); }
        }

        public class PropertyCollection<T>
        {
            private T[] arr;
            public PropertyCollection(T[] t)
            {
                arr = t;
            }

            public T this[int col, int row]
            {
                get { return arr[col + row * 5]; }
                private set { arr[col + row * 5] = value; }
            }
        }

       /// <Property>
       /// 
       /// </summary>
       public int Depth { get { return _Maxdepth; } set { _Maxdepth = value; } }
       public bool TurtoiseIsComputer { get { return _control[0]; } set { _control[0] = value; } }
       public bool HareIsComputer { get { return _control[1]; } set { _control[1] = value; } }
       public string Turn { get { return (_turn == 0) ?  "turtoise" : "hare"; } }
       public PropertyCollection<MapUnit> Map;

        /// <Private data>
        /// 
        /// </summary>
        MapUnit[] _map;
        Piece[] _participant;
        List<int>[,] _TableList;
        int _Maxdepth;
        KeyValuePair<int, int> _bestMove; // item1, id ; item2, dst  if id is -1, represent no action at the turn
        int _turn;
        bool[] _control;                  // false, manual ; true, computer

        public Game() 
        {

            _map = new MapUnit[25];

            for (int i = 0; i < 25; i++) 
            {
                switch (i) 
                {
                    case 0: case 1: case 2: case 3:
                        _map[i] = new MapUnit(MapKind.TtDest); break;

                    case 9: case 14: case 19: case 24:
                        _map[i] = new MapUnit(MapKind.HrDest); break;

                    case 4: case 5: case 10: case 15: case 21: case 22:case 23:
                        _map[i] = new MapUnit(MapKind.Occupy); break;

                    default:
                        _map[i] = new MapUnit(MapKind.Space); break;
                }
            }

            _participant = new Piece[6]
            {
                    new Piece(PieceKind.Turtoise, 21),
                    new Piece(PieceKind.Turtoise, 22),
                    new Piece(PieceKind.Turtoise, 23),
                    new Piece(PieceKind.Rare, 5),
                    new Piece(PieceKind.Rare, 10),
                    new Piece(PieceKind.Rare, 15),                
            };

            _Maxdepth = 16;
            _turn = 0;
            _control = new bool[2] { false,true };
            Map = new PropertyCollection<MapUnit>(_map);

            _TableList = new List<int>[2, 25];

            for(int i=0;i<2;i++)
            {
                for(int j = 0;j < 25;j++)
                {
                    _TableList[i, j] = new List<int>();
                }
            }

            //turtoise's possible step
            for (int i = 5; i < 24; i++) 
            {
                if (i % 5 == 4) continue;
                _TableList[0, i].Add(i-5);
                if( i % 5 != 0) _TableList[0, i].Add(i-1);
                if (i % 5 != 3) _TableList[0, i].Add(i+1);
            }

            //hare's possible step
            for (int i = 5; i < 24; i++)
            {
                if (i % 5 == 4) continue;
                _TableList[1, i].Add(i + 1);
                if (i / 5 != 1) _TableList[1, i].Add(i - 5);
                if (i / 5 != 4) _TableList[1, i].Add(i + 5);
            }
        }

        public bool InteractionAvailable(int col, int row) 
        {
            if (_control[_turn]) return false;
            int index = col + row * 5;
            var p = _participant.First(x => x.Location == index);
            if ((_turn == 0 && p.Kind == PieceKind.Turtoise) ||
                 (_turn == 1 && p.Kind == PieceKind.Rare)) return true;
            else return false;
        }

        public bool InteractionMovePiece(int col, int row, int dcol, int drow) 
        {
            int index = col + row * 5;
            int dindex = dcol + drow * 5;

            try
            {
                _TableList[_turn, index].First(x => x == dindex);
            }
            catch 
            {
                return false;
            }

            var p = _participant.First(x => x.Location == index);
            MovePiece(p, dindex);
            _turn ^= 1;
            return true;
        }

        /// <summary>
        /// item1 = ori col, item2 = ori row, ietm3 = dest col, item4 dest row 
        /// </summary>
        /// <returns></returns>
        public Tuple<int, int, int, int> AutoControlMovePiece() 
        {
            if (_control[_turn]) 
            {
                _bestMove = new KeyValuePair<int, int>(-1, -1);
                ABPruing(_turn, _Maxdepth, -99999, 99999);
                if (_bestMove.Equals(new KeyValuePair<int, int>(-1, -1)))
                {
                    //stuck
                    _turn ^= 1;
                    return null;
                }
                else 
                {
                    int index = _bestMove.Key;
                    int oriCol = _participant[index].Location % 5;
                    int oriRow = _participant[index].Location / 5;
                    MovePiece(_participant[index], _bestMove.Value);
                    int col = _participant[index].Location % 5;
                    int row = _participant[index].Location / 5;
                    _turn ^= 1;
                    return Tuple.Create<int, int, int, int>(oriCol, oriRow, col, row);
                }
            }
            return null;
        }

        public bool InteractionStuckHandle()
        {
            int offset = _turn * 3;
            int size = offset + 3;
            bool stuck = true;
            for (int i = offset; i < size; i++)
            {
                if (_participant[i].Arrived) continue;
                else
                {
                    foreach (int step in _TableList[_turn, _participant[i].Location])
                    {
                        if (_map[step].Status == MapKind.Space) stuck = false;
                    }
                }
            }
            if (stuck)
            {
                _turn ^= 1;
                return true;
            }
            else return false;
        }

        public string InteractionGameOver() 
        {
            bool ttWin = GameOver(0);
            bool HrWin = GameOver(1);

            if(ttWin) return "turtoise";
            if(HrWin) return "hare";
            return null;
        }

        bool MovePiece(Piece p, int dst) 
        {
            _map[p.Location].BeSpaced();
            _map[dst].BeOccupied();
            p.Location = dst;
            p.TestArrival(_map[p.Location].Status);
            return true;
        }

        bool GameOver(int turn)
        {
            int offset = turn * 3;
            int size = offset + 3;
            for (int i = offset; i < size; i++)
            {
                if (_participant[i].Arrived == false) return false;
            }
            return true;
        }

        int Evaluation(int turn) 
        {
            int valueT = 0, valueH = 0;
            for (int i = 0; i < 3; i++)
            {
                valueT += 4 - _participant[i].Location / 5;
                //if (_participant[i].Arrived) valueT += 10;
            }

            for (int i = 3; i < 6; i++)
            {
                valueH += _participant[i].Location % 5;
                //if (_participant[i].Arrived) valueH += 10;
            }

            if (turn == 0) return valueT - valueH;
            else return valueH - valueT;
        }

       int ABPruing(int turn, int depth, int a, int b) 
        {
            if (depth <= 0) return Evaluation(turn);
            if (GameOver(turn)) return -5 + depth;

            bool stuck = true;
            int value, oriLoc;
            int offset = turn * 3;
            int size = offset + 3;

            for (int i = offset; i < size; i++) 
            {
                if (_participant[i].Arrived) continue;
                else 
                {
                    oriLoc = _participant[i].Location;
                    foreach (int dest in _TableList[turn, oriLoc]) 
                    {
                        if (_map[dest].Status == MapKind.Space || _map[dest].Status == _participant[i].destKind) 
                        {
                            stuck = false;
                            MovePiece(_participant[i], dest);     //move
                            value = -ABPruing((turn ^ 1), depth - 1, -b, -a);
                            MovePiece(_participant[i], oriLoc);   //revert
                            if (a < value)
                            {
                                if (depth == _Maxdepth) _bestMove = new KeyValuePair<int, int>(i, dest);
                                a = value;
                            }
                            if (a >= b) return a;
                        }
                    }
                }
            }
            if (stuck == true)
            {
                value = -ABPruing((turn ^ 1), depth - 1, -b, -a);
                a = Math.Max(a, value);
                if (a >= b) return a;
            }
                return a;
        }

    }


}
