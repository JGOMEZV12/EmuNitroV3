using System;
using System.Text;

namespace Polar.HabboHotel.Rooms
{
    public class DynamicRoomModel
    {
        // ─────────────────────────────────────
        //  Campos / propiedades
        // ─────────────────────────────────────
        public bool ClubOnly { get; set; }
        public int DoorOrientation { get; private set; }
        public int DoorX { get; set; }
        public int DoorY { get; set; }

        // FIX: DoorZ se mantenía como double — en el original se casteaba a int perdiendo decimales
        public double DoorZ { get; set; }

        public string Heightmap { get; private set; }
        public int MapSizeX { get; private set; }
        public int MapSizeY { get; private set; }

        public short[,] SqFloorHeight { get; private set; }
        public byte[,] SqSeatRot { get; private set; }
        public SquareState[,] SqState { get; private set; }

        private RoomModel _staticModel;

        // FIX: heightmap relativo ahora se recalcula bajo demanda en lugar de cachearse
        // en el constructor — así refleja cambios de OpenSquare/AddX/AddY correctamente
        private bool _heightmapDirty = true;
        private string _cachedHeightmap = null;

        // ─────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────
        public DynamicRoomModel(RoomModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            _staticModel = model;
            DoorX = _staticModel.DoorX;
            DoorY = _staticModel.DoorY;
            DoorZ = _staticModel.DoorZ;   // FIX: ya no se castea a int
            DoorOrientation = _staticModel.DoorOrientation;
            Heightmap = _staticModel.Heightmap;
            MapSizeX = _staticModel.MapSizeX;
            MapSizeY = _staticModel.MapSizeY;

            SqState = new SquareState[MapSizeX, MapSizeY];
            SqFloorHeight = new short[MapSizeX, MapSizeY];
            SqSeatRot = new byte[MapSizeX, MapSizeY];

            CopyFromStatic();
        }

        // ─────────────────────────────────────
        //  Helpers privados
        // ─────────────────────────────────────
        private void CopyFromStatic()
        {
            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    if (x > _staticModel.MapSizeX - 1 || y > _staticModel.MapSizeY - 1)
                    {
                        SqState[x, y] = SquareState.BLOCKED;
                    }
                    else
                    {
                        SqState[x, y] = _staticModel.SqState[x, y];
                        SqFloorHeight[x, y] = _staticModel.SqFloorHeight[x, y];
                        SqSeatRot[x, y] = _staticModel.SqSeatRot[x, y];
                    }
                }
            }
            _heightmapDirty = true;
        }

        // ─────────────────────────────────────
        //  RefreshArrays
        //  FIX: arrays con tamaño exacto MapSizeX/Y — antes era MapSizeX+1/MapSizeY+1 (incorrecto)
        // ─────────────────────────────────────
        public void RefreshArrays()
        {
            var newSqState = new SquareState[MapSizeX, MapSizeY];
            var newSqFloorHeight = new short[MapSizeX, MapSizeY];
            var newSqSeatRot = new byte[MapSizeX, MapSizeY];

            int prevX = SqState.GetLength(0);
            int prevY = SqState.GetLength(1);

            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    // Si la celda estaba fuera del array anterior, la inicializamos como BLOCKED
                    if (x >= prevX || y >= prevY)
                    {
                        newSqState[x, y] = SquareState.BLOCKED;
                        continue;
                    }

                    if (x > _staticModel.MapSizeX - 1 || y > _staticModel.MapSizeY - 1)
                        newSqState[x, y] = SquareState.BLOCKED;
                    else
                    {
                        newSqState[x, y] = SqState[x, y];
                        newSqFloorHeight[x, y] = SqFloorHeight[x, y];
                        newSqSeatRot[x, y] = SqSeatRot[x, y];
                    }
                }
            }

            SqState = newSqState;
            SqFloorHeight = newSqFloorHeight;
            SqSeatRot = newSqSeatRot;

            _heightmapDirty = true;
        }

        // ─────────────────────────────────────
        //  Heightmap relativo — recalculado solo si hay cambios
        // ─────────────────────────────────────
        public string GetRelativeHeightmap()
        {
            if (!_heightmapDirty && _cachedHeightmap != null)
                return _cachedHeightmap;

            var sb = new StringBuilder();

            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    if (x == DoorX && y == DoorY)
                    {
                        sb.Append(DoorZ > 9
                            ? ((char)(87 + DoorZ)).ToString()
                            : ((int)DoorZ).ToString());
                        continue;
                    }

                    if (SqState[x, y] == SquareState.BLOCKED)
                    {
                        sb.Append('x');
                        continue;
                    }

                    short height = SqFloorHeight[x, y];
                    sb.Append(height > 9
                        ? ((char)(87 + height)).ToString()
                        : height.ToString());
                }
                sb.Append('\r');
            }

            _cachedHeightmap = sb.ToString();
            _heightmapDirty = false;
            return _cachedHeightmap;
        }

        // ─────────────────────────────────────
        //  Mutadores de tamaño
        // ─────────────────────────────────────
        public void AddX()
        {
            MapSizeX++;
            RefreshArrays();
        }

        public void AddY()
        {
            MapSizeY++;
            RefreshArrays();
        }

        public void SetMapsize(int x, int y)
        {
            MapSizeX = x;
            MapSizeY = y;
            RefreshArrays();
        }

        // ─────────────────────────────────────
        //  OpenSquare
        //  FIX: clamp corregido — el límite real es 35 (char 'z'), no 9
        // ─────────────────────────────────────
        public void OpenSquare(int x, int y, double z)
        {
            z = Math.Max(0, Math.Min(z, 35));   // FIX: era Math.Min(z, 9) — incorrecto
            SqFloorHeight[x, y] = (short)z;
            SqState[x, y] = SquareState.OPEN;
            _heightmapDirty = true;
        }

        // ─────────────────────────────────────
        //  Validación de puerta
        // ─────────────────────────────────────
        public bool DoorIsValid()
        {
            return DoorX <= SqFloorHeight.GetUpperBound(0)
                && DoorY <= SqFloorHeight.GetUpperBound(1);
        }

        // ─────────────────────────────────────
        //  Destroy
        // ─────────────────────────────────────
        public void Destroy()
        {
            if (SqState != null) Array.Clear(SqState, 0, SqState.Length);
            if (SqFloorHeight != null) Array.Clear(SqFloorHeight, 0, SqFloorHeight.Length);
            if (SqSeatRot != null) Array.Clear(SqSeatRot, 0, SqSeatRot.Length);

            _staticModel = null;
            Heightmap = null;
            SqState = null;
            SqFloorHeight = null;
            SqSeatRot = null;
            _cachedHeightmap = null;
        }
    }
}