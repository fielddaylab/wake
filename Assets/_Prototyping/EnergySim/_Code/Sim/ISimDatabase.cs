using System;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public interface ISimDatabase : IDisposable, IUpdateVersioned
    {
        SimTypeDatabase<ActorType> Actors { get; }
        SimTypeDatabase<EnvironmentType> Envs { get; }
        
        VarTypeDatabase Vars { get; }
        SimTypeDatabase<VarType> Resources { get; }
        SimTypeDatabase<VarType> Properties { get; }
    }
}