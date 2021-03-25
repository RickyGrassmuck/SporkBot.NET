// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

namespace FlatbuffersResource
{

using global::System;
using global::System.Collections.Generic;
using global::Google.FlatBuffers;

public struct EncounterNest8 : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_1_12_0(); }
  public static EncounterNest8 GetRootAsEncounterNest8(ByteBuffer _bb) { return GetRootAsEncounterNest8(_bb, new EncounterNest8()); }
  public static EncounterNest8 GetRootAsEncounterNest8(ByteBuffer _bb, EncounterNest8 obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public EncounterNest8 __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public uint EntryIndex { get { int o = __p.__offset(4); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }
  public uint Species { get { int o = __p.__offset(6); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }
  public uint AltForm { get { int o = __p.__offset(8); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }
  public ulong LevelTableID { get { int o = __p.__offset(10); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
  public sbyte Ability { get { int o = __p.__offset(12); return o != 0 ? __p.bb.GetSbyte(o + __p.bb_pos) : (sbyte)0; } }
  public bool IsGigantamax { get { int o = __p.__offset(14); return o != 0 ? 0!=__p.bb.Get(o + __p.bb_pos) : (bool)false; } }
  public ulong DropTableID { get { int o = __p.__offset(16); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
  public ulong BonusTableID { get { int o = __p.__offset(18); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
  public uint Probabilities(int j) { int o = __p.__offset(20); return o != 0 ? __p.bb.GetUint(__p.__vector(o) + j * 4) : (uint)0; }
  public int ProbabilitiesLength { get { int o = __p.__offset(20); return o != 0 ? __p.__vector_len(o) : 0; } }
  public Span<uint> GetProbabilitiesBytes() { return __p.__vector_as_span<uint>(20, 4); }
  public uint[] GetProbabilitiesArray() { return __p.__vector_as_array<uint>(20); }
  public sbyte Gender { get { int o = __p.__offset(22); return o != 0 ? __p.bb.GetSbyte(o + __p.bb_pos) : (sbyte)0; } }
  public sbyte FlawlessIVs { get { int o = __p.__offset(24); return o != 0 ? __p.bb.GetSbyte(o + __p.bb_pos) : (sbyte)0; } }

  public static Offset<FlatbuffersResource.EncounterNest8> CreateEncounterNest8(FlatBufferBuilder builder,
      uint EntryIndex = 0,
      uint Species = 0,
      uint AltForm = 0,
      ulong LevelTableID = 0,
      sbyte Ability = 0,
      bool IsGigantamax = false,
      ulong DropTableID = 0,
      ulong BonusTableID = 0,
      VectorOffset ProbabilitiesOffset = default(VectorOffset),
      sbyte Gender = 0,
      sbyte FlawlessIVs = 0) {
    builder.StartTable(11);
    EncounterNest8.AddBonusTableID(builder, BonusTableID);
    EncounterNest8.AddDropTableID(builder, DropTableID);
    EncounterNest8.AddLevelTableID(builder, LevelTableID);
    EncounterNest8.AddProbabilities(builder, ProbabilitiesOffset);
    EncounterNest8.AddAltForm(builder, AltForm);
    EncounterNest8.AddSpecies(builder, Species);
    EncounterNest8.AddEntryIndex(builder, EntryIndex);
    EncounterNest8.AddFlawlessIVs(builder, FlawlessIVs);
    EncounterNest8.AddGender(builder, Gender);
    EncounterNest8.AddIsGigantamax(builder, IsGigantamax);
    EncounterNest8.AddAbility(builder, Ability);
    return EncounterNest8.EndEncounterNest8(builder);
  }

  public static void StartEncounterNest8(FlatBufferBuilder builder) { builder.StartTable(11); }
  public static void AddEntryIndex(FlatBufferBuilder builder, uint EntryIndex) { builder.AddUint(0, EntryIndex, 0); }
  public static void AddSpecies(FlatBufferBuilder builder, uint Species) { builder.AddUint(1, Species, 0); }
  public static void AddAltForm(FlatBufferBuilder builder, uint AltForm) { builder.AddUint(2, AltForm, 0); }
  public static void AddLevelTableID(FlatBufferBuilder builder, ulong LevelTableID) { builder.AddUlong(3, LevelTableID, 0); }
  public static void AddAbility(FlatBufferBuilder builder, sbyte Ability) { builder.AddSbyte(4, Ability, 0); }
  public static void AddIsGigantamax(FlatBufferBuilder builder, bool IsGigantamax) { builder.AddBool(5, IsGigantamax, false); }
  public static void AddDropTableID(FlatBufferBuilder builder, ulong DropTableID) { builder.AddUlong(6, DropTableID, 0); }
  public static void AddBonusTableID(FlatBufferBuilder builder, ulong BonusTableID) { builder.AddUlong(7, BonusTableID, 0); }
  public static void AddProbabilities(FlatBufferBuilder builder, VectorOffset ProbabilitiesOffset) { builder.AddOffset(8, ProbabilitiesOffset.Value, 0); }
  public static VectorOffset CreateProbabilitiesVector(FlatBufferBuilder builder, uint[] data) { builder.StartVector(4, data.Length, 4); for (int i = data.Length - 1; i >= 0; i--) builder.AddUint(data[i]); return builder.EndVector(); }
  public static VectorOffset CreateProbabilitiesVectorBlock(FlatBufferBuilder builder, uint[] data) { builder.StartVector(4, data.Length, 4); builder.Add(data); return builder.EndVector(); }
  public static void StartProbabilitiesVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
  public static void AddGender(FlatBufferBuilder builder, sbyte Gender) { builder.AddSbyte(9, Gender, 0); }
  public static void AddFlawlessIVs(FlatBufferBuilder builder, sbyte FlawlessIVs) { builder.AddSbyte(10, FlawlessIVs, 0); }
  public static Offset<FlatbuffersResource.EncounterNest8> EndEncounterNest8(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<FlatbuffersResource.EncounterNest8>(o);
  }
};


}
