using LineSystem;

namespace TerrainEditing
{
    public enum CoordinateState
    {
        Free = 0, // Free to build on
        HeightSet, // Height has been set, but nothing is built on it yet
        Occupied // Occupied by an object
    }

    public abstract class CoordinateStateHolder
    {        
        public abstract CoordinateState GetState();
    }

    public class FreeCoordinateState : CoordinateStateHolder
    {
        public override CoordinateState GetState()
        {
            return CoordinateState.Free;
        }
    }

    public class HeightSetCoordinateState : CoordinateStateHolder
    {
        public override CoordinateState GetState()
        {
            return CoordinateState.HeightSet;
        }

    }
    public class OccupiedCoordinateState : CoordinateStateHolder
    {
        public ILineElement OccupyingElement { get; private set; }
        public OccupiedCoordinateState(ILineElement occupyingElement)
        {
            OccupyingElement = occupyingElement;
        }
        public override CoordinateState GetState()
        {
            return CoordinateState.Occupied;
        }
    }
}