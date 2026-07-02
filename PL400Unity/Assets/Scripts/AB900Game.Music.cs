using System.Collections.Generic;
using UnityEngine;

// Parte de AB900Game dedicada al AUDIO: chiptune ORIGINAL generado por código (CC0).
// Identidad: JRPG de 16 bits con vibra Final Fantasy IV/V/VI + Pokémon GBA, pero 100%
// original (sin reproducir melodías reconocibles de Nintendo/Square). Se separó del
// archivo principal por legibilidad; sigue siendo la MISMA clase (partial), así que
// comparte campos (musicSrc/sfxSrc/currentTrack/musicClips/sfx*) declarados en AB900Game.cs.
//
// Estructura: motivos (int[] de notas MIDI; 0 = silencio) -> arrays de pista
// (TITLE_/TOWN_/D1_/D2_/D3_/FIN_/BAT_/BOSS_/SAGE_) -> PlayTrack -> BuildTrack (nombre+tempo)
// -> BuildMusic (mezcla lead/bajo/armonía/percusión + eco + limitador) -> FillTone/FillDrum.
// Lead = pulso con 2º armónico (brillo); bajo = triángulo; armonía = seno con caída de arpa.
public sealed partial class AB900Game
{
    // ---------------- Audio: chiptune original generado por código (CC0) ----------------

    // 0 = silencio. Cada canción usa motivo + respuesta + variación + clímax para evitar
    // bucles cortos, y suma bajo, armonía (arpegios) y percusión en capas.
    static int[] Seq(params int[][] parts)
    {
        var list = new List<int>();
        foreach (var p in parts) list.AddRange(p);
        return list.ToArray();
    }

    const int Kick = 1, Snare = 2, Hat = 3, Crash = 4;

    // --- Título: himno noble en Do mayor (vibra apertura FFVI), arpegios y clímax alto ---
    // Acordes por motivo: A = C(I)->G(V) · B = Am(vi)->F(IV) · C = F(IV)->G(V)->C(I) clímax.
    static readonly int[] tA_l = { 72, 0, 71, 72, 76, 0, 79, 0, 77, 76, 74, 72, 74, 0, 67, 0 };
    static readonly int[] tB_l = { 69, 71, 72, 0, 76, 0, 74, 72, 71, 69, 67, 69, 71, 0, 72, 0 };
    static readonly int[] tC_l = { 77, 79, 81, 0, 84, 0, 83, 81, 79, 81, 84, 0, 88, 0, 72, 0 };
    static readonly int[] tA_b = { 48, 0, 55, 0, 52, 0, 55, 0, 43, 0, 50, 0, 47, 0, 43, 0 };
    static readonly int[] tB_b = { 45, 0, 52, 0, 48, 0, 52, 0, 41, 0, 48, 0, 45, 0, 40, 0 };
    static readonly int[] tC_b = { 41, 0, 48, 0, 43, 0, 50, 0, 48, 0, 55, 0, 36, 0, 48, 0 };
    static readonly int[] tA_h = { 64, 67, 72, 67, 64, 67, 72, 67, 62, 67, 71, 67, 62, 67, 71, 0 };
    static readonly int[] tB_h = { 60, 64, 69, 64, 60, 64, 69, 64, 60, 65, 69, 65, 59, 62, 67, 0 };
    static readonly int[] tC_h = { 65, 69, 72, 69, 62, 67, 71, 67, 64, 67, 72, 67, 64, 67, 76, 0 };
    static readonly int[] tDr = { Kick, 0, Hat, 0, Snare, 0, Hat, Hat, Kick, 0, Hat, 0, Snare, 0, Hat, 0 };
    static readonly int[] TITLE_LEAD = Seq(tA_l, tB_l, tA_l, tC_l, tA_l, tB_l, tC_l, tC_l);
    static readonly int[] TITLE_BASS = Seq(tA_b, tB_b, tA_b, tC_b, tA_b, tB_b, tC_b, tC_b);
    static readonly int[] TITLE_HARM = Seq(tA_h, tB_h, tA_h, tC_h, tA_h, tB_h, tC_h, tC_h);
    static readonly int[] TITLE_DRUMS = Seq(tDr, tDr, tDr, tDr, tDr, tDr, tDr, tDr);

    // --- Pueblo: Fa mayor luminoso y cantable (paseo tipo FFV / ruta Pokémon GBA) ---
    static readonly int[] nA_l = { 72, 74, 76, 77, 76, 74, 72, 0, 69, 72, 74, 72, 70, 69, 67, 0 };
    static readonly int[] nB_l = { 74, 76, 77, 79, 77, 76, 74, 0, 72, 70, 69, 67, 69, 0, 65, 0 };
    static readonly int[] nC_l = { 77, 79, 81, 82, 81, 79, 77, 76, 74, 76, 77, 79, 81, 0, 72, 0 };
    static readonly int[] nA_b = { 41, 0, 48, 0, 48, 0, 55, 0, 50, 0, 45, 0, 43, 0, 48, 0 };
    static readonly int[] nB_b = { 50, 0, 45, 0, 46, 0, 41, 0, 41, 0, 48, 0, 43, 0, 48, 0 };
    static readonly int[] nC_b = { 46, 0, 41, 0, 48, 0, 43, 0, 41, 0, 48, 0, 53, 0, 41, 0 };
    static readonly int[] nA_h = { 65, 69, 72, 69, 65, 69, 72, 69, 62, 69, 74, 69, 67, 72, 76, 0 };
    static readonly int[] nB_h = { 62, 65, 69, 65, 62, 65, 69, 65, 60, 65, 69, 65, 64, 67, 72, 0 };
    static readonly int[] nC_h = { 65, 70, 74, 70, 67, 72, 76, 72, 65, 69, 72, 69, 65, 69, 72, 0 };
    static readonly int[] nDr = { Kick, 0, Hat, Hat, Snare, 0, Hat, 0, Kick, 0, Hat, Hat, Snare, 0, Hat, Hat };
    static readonly int[] TOWN_LEAD = Seq(nA_l, nB_l, nA_l, nC_l, nA_l, nB_l, nC_l, nB_l);
    static readonly int[] TOWN_BASS = Seq(nA_b, nB_b, nA_b, nC_b, nA_b, nB_b, nC_b, nB_b);
    static readonly int[] TOWN_HARM = Seq(nA_h, nB_h, nA_h, nC_h, nA_h, nB_h, nC_h, nB_h);
    static readonly int[] TOWN_DRUMS = Seq(nDr, nDr, nDr, nDr, nDr, nDr, nDr, nDr);

    // --- Lectura/Granja: estudio sereno en Do mayor, piano suave (lead Sine con pluck), SIN
    //     percusión, tempo lento. Compartida por la lectura de tomos/súper tomos y la Granja. ---
    static readonly int[] tmA_l = { 76, 0, 0, 72, 74, 0, 0, 67, 69, 0, 0, 72, 71, 0, 0, 0 };
    static readonly int[] tmB_l = { 77, 0, 0, 72, 74, 0, 0, 69, 67, 0, 0, 71, 74, 0, 0, 0 };
    static readonly int[] tmC_l = { 79, 0, 76, 0, 72, 0, 74, 76, 77, 0, 76, 74, 72, 0, 0, 0 };
    static readonly int[] tmA_b = { 48, 0, 0, 0, 55, 0, 0, 0, 45, 0, 0, 0, 52, 0, 0, 0 };
    static readonly int[] tmB_b = { 41, 0, 0, 0, 48, 0, 0, 0, 43, 0, 0, 0, 50, 0, 0, 0 };
    static readonly int[] tmC_b = { 48, 0, 0, 0, 43, 0, 0, 0, 45, 0, 0, 0, 41, 0, 0, 0 };
    static readonly int[] tmA_h = { 60, 64, 67, 0, 60, 64, 67, 0, 57, 60, 64, 0, 57, 60, 64, 0 };
    static readonly int[] tmB_h = { 53, 57, 60, 0, 53, 57, 60, 0, 55, 59, 62, 0, 55, 59, 62, 0 };
    static readonly int[] tmC_h = { 60, 64, 67, 72, 55, 59, 62, 67, 57, 60, 64, 69, 53, 57, 60, 65 };
    static readonly int[] TOME_LEAD = Seq(tmA_l, tmB_l, tmA_l, tmC_l, tmA_l, tmB_l, tmC_l, tmB_l);
    static readonly int[] TOME_BASS = Seq(tmA_b, tmB_b, tmA_b, tmC_b, tmA_b, tmB_b, tmC_b, tmB_b);
    static readonly int[] TOME_HARM = Seq(tmA_h, tmB_h, tmA_h, tmC_h, tmA_h, tmB_h, tmC_h, tmB_h);

    // --- d1 Bastión: La menor misteriosa y lenta, con G# (tono sensible) (miedo nivel 1) ---
    static readonly int[] d1A_l = { 69, 0, 72, 0, 71, 0, 69, 67, 69, 0, 71, 0, 72, 0, 0, 0 };
    static readonly int[] d1B_l = { 72, 0, 76, 0, 74, 0, 72, 71, 69, 0, 67, 0, 69, 0, 0, 0 };
    static readonly int[] d1C_l = { 76, 0, 74, 72, 71, 0, 69, 71, 72, 0, 71, 69, 68, 0, 69, 0 };
    static readonly int[] d1A_b = { 45, 0, 0, 52, 41, 0, 0, 48, 43, 0, 0, 50, 40, 0, 0, 45 };
    static readonly int[] d1B_b = { 48, 0, 0, 55, 45, 0, 0, 52, 43, 0, 0, 50, 40, 0, 0, 40 };
    static readonly int[] d1C_b = { 41, 0, 0, 48, 43, 0, 0, 50, 45, 0, 0, 52, 40, 0, 45, 0 };
    static readonly int[] d1A_h = { 64, 0, 69, 0, 60, 0, 65, 0, 62, 0, 67, 0, 64, 0, 0, 0 };
    static readonly int[] d1B_h = { 67, 0, 72, 0, 64, 0, 69, 0, 62, 0, 67, 0, 64, 0, 0, 0 };
    static readonly int[] d1C_h = { 72, 0, 69, 0, 67, 0, 64, 0, 65, 0, 62, 0, 68, 0, 64, 0 };
    static readonly int[] d1Dr = { Kick, 0, 0, 0, 0, 0, Snare, 0, Kick, 0, 0, Hat, 0, 0, Snare, 0 };
    static readonly int[] D1_LEAD = Seq(d1A_l, d1B_l, d1A_l, d1C_l, d1A_l, d1B_l, d1C_l, d1A_l);
    static readonly int[] D1_BASS = Seq(d1A_b, d1B_b, d1A_b, d1C_b, d1A_b, d1B_b, d1C_b, d1A_b);
    static readonly int[] D1_HARM = Seq(d1A_h, d1B_h, d1A_h, d1C_h, d1A_h, d1B_h, d1C_h, d1A_h);
    static readonly int[] D1_DRUMS = Seq(d1Dr, d1Dr, d1Dr, d1Dr, d1Dr, d1Dr, d1Dr, d1Dr);

    // --- d2 Cripta: Mi frigio (b2 = escalofrío), tensa, tipo ruinas FFVI (miedo nivel 2) ---
    static readonly int[] d2A_l = { 64, 0, 65, 67, 65, 64, 0, 62, 64, 0, 65, 64, 62, 0, 64, 0 };
    static readonly int[] d2B_l = { 71, 0, 72, 71, 69, 67, 65, 67, 64, 0, 65, 64, 62, 0, 60, 0 };
    static readonly int[] d2C_l = { 76, 0, 75, 76, 72, 71, 0, 69, 71, 72, 71, 69, 67, 65, 64, 0 };
    static readonly int[] d2A_b = { 40, 0, 40, 41, 40, 0, 38, 0, 40, 0, 41, 40, 36, 0, 38, 0 };
    static readonly int[] d2B_b = { 45, 0, 0, 41, 43, 0, 0, 40, 41, 0, 0, 40, 36, 0, 40, 0 };
    static readonly int[] d2C_b = { 48, 0, 0, 41, 43, 0, 0, 40, 41, 0, 0, 40, 41, 40, 38, 0 };
    static readonly int[] d2A_h = { 67, 71, 67, 0, 65, 69, 65, 0, 64, 67, 64, 0, 62, 67, 0, 0 };
    static readonly int[] d2B_h = { 71, 74, 71, 0, 67, 71, 67, 0, 65, 69, 65, 0, 64, 67, 0, 0 };
    static readonly int[] d2C_h = { 72, 76, 72, 0, 71, 74, 71, 0, 67, 71, 67, 0, 64, 67, 71, 0 };
    static readonly int[] d2Dr = { Kick, 0, Hat, 0, Kick, 0, Snare, 0, Kick, Kick, Hat, 0, Snare, 0, Hat, Hat };
    static readonly int[] D2_LEAD = Seq(d2A_l, d2A_l, d2B_l, d2A_l, d2C_l, d2A_l, d2B_l, d2C_l);
    static readonly int[] D2_BASS = Seq(d2A_b, d2A_b, d2B_b, d2A_b, d2C_b, d2A_b, d2B_b, d2C_b);
    static readonly int[] D2_HARM = Seq(d2A_h, d2A_h, d2B_h, d2A_h, d2C_h, d2A_h, d2B_h, d2C_h);
    static readonly int[] D2_DRUMS = Seq(d2Dr, d2Dr, d2Dr, d2Dr, d2Dr, d2Dr, d2Dr, d2Dr);

    // --- d3 Fábrica: Re menor mecánica, bajo industrial en corcheas + tritono G# (nivel 3) ---
    static readonly int[] d3A_l = { 62, 0, 65, 67, 68, 67, 65, 0, 62, 0, 65, 62, 60, 0, 62, 0 };
    static readonly int[] d3B_l = { 69, 0, 70, 69, 67, 65, 67, 0, 65, 0, 62, 65, 64, 0, 62, 0 };
    static readonly int[] d3C_l = { 74, 0, 72, 70, 69, 68, 67, 65, 67, 0, 65, 62, 64, 62, 60, 0 };
    static readonly int[] d3A_b = { 38, 38, 38, 38, 36, 36, 36, 36, 34, 34, 34, 34, 33, 33, 32, 32 };
    static readonly int[] d3B_b = { 41, 41, 41, 41, 40, 40, 40, 40, 38, 38, 38, 38, 36, 36, 36, 36 };
    static readonly int[] d3C_b = { 38, 38, 38, 38, 33, 33, 33, 33, 34, 34, 34, 34, 38, 38, 38, 38 };
    static readonly int[] d3A_h = { 65, 69, 72, 69, 65, 68, 72, 68, 62, 65, 69, 65, 64, 67, 70, 0 };
    static readonly int[] d3B_h = { 69, 72, 76, 72, 67, 70, 74, 70, 65, 69, 72, 69, 64, 67, 70, 0 };
    static readonly int[] d3C_h = { 74, 77, 81, 77, 69, 72, 76, 72, 65, 69, 72, 69, 64, 67, 70, 0 };
    static readonly int[] d3Dr = { Kick, Hat, Kick, Hat, Snare, Hat, Kick, Hat, Kick, Kick, Snare, Hat, Snare, Hat, Kick, Hat };
    static readonly int[] D3_LEAD = Seq(d3A_l, d3B_l, d3A_l, d3C_l, d3A_l, d3B_l, d3C_l, d3C_l);
    static readonly int[] D3_BASS = Seq(d3A_b, d3B_b, d3A_b, d3C_b, d3A_b, d3B_b, d3C_b, d3C_b);
    static readonly int[] D3_HARM = Seq(d3A_h, d3B_h, d3A_h, d3C_h, d3A_h, d3B_h, d3C_h, d3C_h);
    static readonly int[] D3_DRUMS = Seq(d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr);

    // --- Torre final: La frigio grave, órgano y pedal bajo, marcha pesada (miedo máximo) ---
    static readonly int[] fnA_l = { 69, 0, 70, 69, 72, 0, 70, 69, 67, 0, 69, 70, 69, 0, 0, 0 };
    static readonly int[] fnB_l = { 72, 0, 74, 72, 70, 0, 69, 67, 69, 0, 70, 69, 67, 0, 0, 0 };
    static readonly int[] fnC_l = { 77, 0, 76, 74, 72, 0, 70, 69, 70, 72, 74, 72, 70, 69, 67, 0 };
    static readonly int[] fnA_b = { 33, 0, 33, 34, 33, 0, 0, 33, 28, 0, 28, 29, 33, 0, 0, 0 };
    static readonly int[] fnB_b = { 36, 0, 36, 38, 34, 0, 0, 33, 29, 0, 29, 28, 33, 0, 0, 0 };
    static readonly int[] fnC_b = { 41, 0, 0, 40, 36, 0, 0, 33, 34, 0, 0, 33, 33, 0, 33, 0 };
    static readonly int[] fnA_h = { 60, 64, 69, 0, 60, 65, 69, 0, 57, 60, 64, 0, 60, 64, 0, 0 };
    static readonly int[] fnB_h = { 62, 65, 69, 0, 60, 64, 69, 0, 58, 62, 65, 0, 60, 64, 0, 0 };
    static readonly int[] fnC_h = { 65, 69, 72, 0, 60, 64, 69, 0, 62, 65, 70, 0, 60, 64, 69, 0 };
    static readonly int[] fnDr = { Kick, 0, 0, 0, Kick, 0, Snare, 0, Kick, 0, 0, 0, Snare, 0, Crash, 0 };
    static readonly int[] FIN_LEAD = Seq(fnA_l, fnB_l, fnA_l, fnC_l, fnA_l, fnB_l, fnC_l, fnC_l);
    static readonly int[] FIN_BASS = Seq(fnA_b, fnB_b, fnA_b, fnC_b, fnA_b, fnB_b, fnC_b, fnC_b);
    static readonly int[] FIN_HARM = Seq(fnA_h, fnB_h, fnA_h, fnC_h, fnA_h, fnB_h, fnC_h, fnC_h);
    static readonly int[] FIN_DRUMS = Seq(fnDr, fnDr, fnDr, fnDr, fnDr, fnDr, fnDr, fnDr);

    // --- Combate normal: La menor rápido, bajo galopante (gallop) tipo batalla SNES ---
    static readonly int[] bA_l = { 76, 0, 77, 76, 74, 72, 71, 72, 74, 0, 76, 74, 72, 71, 69, 0 };
    static readonly int[] bB_l = { 79, 0, 81, 79, 77, 76, 74, 76, 72, 0, 74, 72, 71, 69, 67, 0 };
    static readonly int[] bC_l = { 72, 74, 76, 77, 79, 81, 79, 77, 76, 74, 72, 71, 69, 71, 72, 0 };
    static readonly int[] bA_b = { 45, 52, 45, 52, 43, 50, 43, 50, 41, 48, 41, 48, 40, 47, 40, 47 };
    static readonly int[] bB_b = { 41, 48, 41, 48, 43, 50, 43, 50, 45, 52, 45, 52, 40, 47, 40, 40 };
    static readonly int[] bC_b = { 41, 48, 41, 48, 40, 47, 40, 47, 45, 52, 45, 52, 43, 50, 43, 0 };
    static readonly int[] bA_h = { 64, 0, 69, 0, 65, 0, 69, 0, 64, 0, 67, 0, 62, 0, 65, 0 };
    static readonly int[] bB_h = { 65, 0, 69, 0, 64, 0, 67, 0, 62, 0, 65, 0, 60, 0, 64, 0 };
    static readonly int[] bC_h = { 60, 0, 64, 0, 64, 0, 67, 0, 65, 0, 69, 0, 64, 0, 67, 0 };
    static readonly int[] bDr = { Kick, Hat, Snare, Hat, Kick, Kick, Snare, Hat, Kick, Hat, Snare, Hat, Kick, Snare, Hat, Snare };
    static readonly int[] BAT_LEAD = Seq(bA_l, bB_l, bA_l, bC_l, bA_l, bB_l, bC_l, bB_l);
    static readonly int[] BAT_BASS = Seq(bA_b, bB_b, bA_b, bC_b, bA_b, bB_b, bC_b, bB_b);
    static readonly int[] BAT_HARM = Seq(bA_h, bB_h, bA_h, bC_h, bA_h, bB_h, bC_h, bB_h);
    static readonly int[] BAT_DRUMS = Seq(bDr, bDr, bDr, bDr, bDr, bDr, bDr, bDr);

    // --- Jefe: Re menor épico, bajo galopante, subida a clímax (D climax alto) ---
    static readonly int[] boA_l = { 74, 0, 77, 0, 76, 74, 72, 74, 70, 0, 69, 70, 72, 0, 74, 0 };
    static readonly int[] boB_l = { 81, 0, 79, 77, 76, 74, 72, 74, 77, 0, 76, 74, 72, 71, 69, 0 };
    static readonly int[] boC_l = { 69, 72, 74, 77, 81, 0, 79, 77, 74, 77, 81, 84, 86, 0, 81, 0 };
    static readonly int[] boD_l = { 77, 76, 74, 72, 70, 69, 67, 69, 70, 72, 74, 77, 76, 74, 72, 0 };
    static readonly int[] boA_b = { 38, 45, 38, 45, 38, 45, 38, 45, 34, 41, 34, 41, 36, 43, 36, 43 };
    static readonly int[] boB_b = { 41, 48, 41, 48, 38, 45, 38, 45, 43, 50, 43, 50, 38, 45, 38, 45 };
    static readonly int[] boC_b = { 38, 45, 38, 45, 41, 48, 41, 48, 43, 50, 43, 50, 38, 45, 38, 38 };
    static readonly int[] boD_b = { 41, 48, 41, 48, 34, 41, 34, 41, 43, 50, 43, 50, 38, 45, 38, 0 };
    static readonly int[] boA_h = { 65, 69, 74, 69, 65, 69, 74, 69, 62, 65, 70, 65, 67, 70, 74, 0 };
    static readonly int[] boB_h = { 69, 72, 77, 72, 65, 69, 74, 69, 70, 74, 77, 74, 65, 69, 72, 0 };
    static readonly int[] boC_h = { 65, 69, 74, 69, 69, 72, 77, 72, 74, 77, 81, 77, 79, 74, 69, 0 };
    static readonly int[] boD_h = { 69, 72, 77, 72, 62, 65, 70, 65, 70, 74, 77, 74, 69, 65, 62, 0 };
    static readonly int[] boDr = { Crash, Hat, Kick, Hat, Snare, Hat, Kick, Hat, Kick, Kick, Snare, Hat, Kick, Snare, Snare, Hat };
    static readonly int[] BOSS_LEAD = Seq(boA_l, boB_l, boA_l, boC_l, boD_l, boB_l, boC_l, boD_l);
    static readonly int[] BOSS_BASS = Seq(boA_b, boB_b, boA_b, boC_b, boD_b, boB_b, boC_b, boD_b);
    static readonly int[] BOSS_HARM = Seq(boA_h, boB_h, boA_h, boC_h, boD_h, boB_h, boC_h, boD_h);
    static readonly int[] BOSS_DRUMS = Seq(boDr, boDr, boDr, boDr, boDr, boDr, boDr, boDr);

    // --- Sabio: La eolio solemne y emotivo, arpa (arpegios) y bajo en pedal (escena clave) ---
    static readonly int[] sgA_l = { 69, 0, 72, 0, 76, 0, 72, 71, 72, 0, 74, 0, 71, 0, 69, 0 };
    static readonly int[] sgB_l = { 72, 0, 76, 0, 79, 0, 76, 74, 77, 0, 76, 74, 72, 0, 71, 0 };
    static readonly int[] sgC_l = { 81, 0, 79, 77, 76, 0, 77, 79, 76, 0, 74, 72, 71, 69, 71, 0 };
    static readonly int[] sgA_b = { 45, 0, 0, 52, 41, 0, 0, 48, 48, 0, 0, 55, 43, 0, 0, 50 };
    static readonly int[] sgB_b = { 45, 0, 0, 52, 41, 0, 0, 48, 48, 0, 0, 55, 43, 0, 0, 47 };
    static readonly int[] sgC_b = { 41, 0, 0, 48, 43, 0, 0, 50, 45, 0, 0, 52, 43, 0, 0, 43 };
    static readonly int[] sgA_h = { 57, 60, 64, 69, 53, 57, 60, 65, 60, 64, 67, 72, 55, 59, 62, 67 };
    static readonly int[] sgB_h = { 57, 60, 64, 69, 53, 57, 60, 65, 60, 64, 67, 72, 55, 59, 62, 67 };
    static readonly int[] sgC_h = { 53, 57, 60, 65, 55, 59, 62, 67, 60, 64, 67, 72, 55, 59, 62, 67 };
    static readonly int[] sgDr = { Crash, 0, 0, 0, 0, 0, 0, 0, Kick, 0, 0, 0, 0, 0, Snare, 0 };
    static readonly int[] SAGE_LEAD = Seq(sgA_l, sgB_l, sgA_l, sgC_l, sgA_l, sgB_l, sgC_l, sgB_l);
    static readonly int[] SAGE_BASS = Seq(sgA_b, sgB_b, sgA_b, sgC_b, sgA_b, sgB_b, sgC_b, sgB_b);
    static readonly int[] SAGE_HARM = Seq(sgA_h, sgB_h, sgA_h, sgC_h, sgA_h, sgB_h, sgC_h, sgB_h);
    static readonly int[] SAGE_DRUMS = Seq(sgDr, sgDr, sgDr, sgDr, sgDr, sgDr, sgDr, sgDr);

    // --- Gran Sabio (piso 18, jefe final): Re menor frigio (Eb = b2 de pavor) + tritono,
    //     bajo galopante grave, marcha con crashes y clímax muy agudo. Épico y aterrador. ---
    static readonly int[] gsA_l = { 74, 0, 75, 74, 70, 0, 69, 67, 65, 0, 67, 65, 62, 0, 0, 0 };
    static readonly int[] gsB_l = { 81, 0, 80, 81, 77, 0, 76, 74, 70, 0, 69, 67, 65, 0, 62, 0 };
    static readonly int[] gsC_l = { 69, 74, 77, 81, 86, 0, 84, 81, 77, 81, 86, 89, 86, 0, 81, 0 };
    static readonly int[] gsD_l = { 77, 76, 74, 72, 70, 69, 67, 65, 63, 0, 65, 63, 62, 0, 0, 0 };
    static readonly int[] gsA_b = { 38, 45, 38, 45, 39, 46, 39, 46, 36, 43, 36, 43, 38, 44, 38, 44 };
    static readonly int[] gsB_b = { 38, 45, 38, 45, 33, 40, 33, 40, 31, 38, 31, 38, 36, 43, 36, 43 };
    static readonly int[] gsC_b = { 38, 45, 38, 45, 36, 43, 36, 43, 33, 40, 33, 40, 38, 45, 38, 38 };
    static readonly int[] gsD_b = { 38, 45, 38, 45, 39, 46, 39, 46, 36, 43, 36, 43, 38, 44, 38, 0 };
    static readonly int[] gsA_h = { 62, 65, 69, 65, 62, 65, 69, 65, 63, 67, 70, 67, 62, 68, 0, 0 };
    static readonly int[] gsB_h = { 69, 72, 77, 72, 65, 69, 74, 69, 62, 65, 70, 65, 64, 68, 0, 0 };
    static readonly int[] gsC_h = { 69, 74, 77, 81, 72, 77, 81, 84, 65, 69, 74, 77, 69, 72, 77, 0 };
    static readonly int[] gsD_h = { 65, 69, 72, 69, 63, 67, 70, 67, 62, 65, 69, 65, 62, 68, 65, 0 };
    static readonly int[] gsDr = { Crash, 0, Kick, Hat, Snare, 0, Kick, Hat, Kick, Kick, Snare, Hat, Kick, Snare, Crash, Hat };
    static readonly int[] GS_LEAD = Seq(gsA_l, gsB_l, gsA_l, gsC_l, gsD_l, gsB_l, gsC_l, gsD_l);
    static readonly int[] GS_BASS = Seq(gsA_b, gsB_b, gsA_b, gsC_b, gsD_b, gsB_b, gsC_b, gsD_b);
    static readonly int[] GS_HARM = Seq(gsA_h, gsB_h, gsA_h, gsC_h, gsD_h, gsB_h, gsC_h, gsD_h);
    static readonly int[] GS_DRUMS = Seq(gsDr, gsDr, gsDr, gsDr, gsDr, gsDr, gsDr, gsDr);

    // --- Torre del Gran Sabio (recorrido): METAL épico y desafiante. Mi frigio dominante
    //     (F natural = b2 + G# = 3ª mayor: sabor exótico/neoclásico), bajo + armonía al
    //     UNÍSONO rítmico (power-chords "chug" de guitarra), batería metalera (doble bombo +
    //     crashes), lead agudo tipo solo de guitarra. Tempo rápido. ---
    static readonly int[] twA_l = { 76, 0, 77, 80, 76, 0, 72, 71, 69, 0, 68, 69, 71, 0, 76, 0 };
    static readonly int[] twB_l = { 83, 0, 81, 80, 77, 0, 76, 72, 71, 0, 69, 68, 69, 0, 71, 0 };
    static readonly int[] twC_l = { 81, 84, 88, 0, 89, 0, 88, 84, 81, 84, 88, 91, 88, 0, 84, 0 };
    static readonly int[] twD_l = { 77, 76, 74, 72, 71, 69, 68, 69, 71, 72, 76, 77, 76, 0, 0, 0 };
    static readonly int[] twA_b = { 40, 40, 40, 0, 40, 40, 40, 0, 41, 41, 41, 0, 40, 40, 40, 0 };
    static readonly int[] twB_b = { 45, 45, 45, 0, 44, 44, 44, 0, 43, 43, 43, 0, 40, 40, 40, 0 };
    static readonly int[] twC_b = { 40, 40, 40, 0, 36, 36, 36, 0, 33, 33, 33, 0, 40, 40, 40, 0 };
    static readonly int[] twD_b = { 40, 40, 40, 0, 41, 41, 41, 0, 44, 44, 44, 0, 45, 45, 45, 0 };
    static readonly int[] twA_h = { 47, 47, 47, 0, 47, 47, 47, 0, 48, 48, 48, 0, 47, 47, 47, 0 };
    static readonly int[] twB_h = { 52, 52, 52, 0, 51, 51, 51, 0, 50, 50, 50, 0, 47, 47, 47, 0 };
    static readonly int[] twC_h = { 47, 47, 47, 0, 43, 43, 43, 0, 40, 40, 40, 0, 47, 47, 47, 0 };
    static readonly int[] twD_h = { 47, 47, 47, 0, 48, 48, 48, 0, 51, 51, 51, 0, 52, 52, 52, 0 };
    static readonly int[] twDr = { Crash, Hat, Kick, Hat, Snare, Hat, Kick, Kick, Kick, Hat, Snare, Hat, Kick, Snare, Kick, Hat };
    static readonly int[] TWR_LEAD = Seq(twA_l, twB_l, twA_l, twC_l, twD_l, twB_l, twC_l, twD_l);
    static readonly int[] TWR_BASS = Seq(twA_b, twB_b, twA_b, twC_b, twD_b, twB_b, twC_b, twD_b);
    static readonly int[] TWR_HARM = Seq(twA_h, twB_h, twA_h, twC_h, twD_h, twB_h, twC_h, twD_h);
    static readonly int[] TWR_DRUMS = Seq(twDr, twDr, twDr, twDr, twDr, twDr, twDr, twDr);

    // --- Torre del Estudio (RECORRIDO): ROCK/METAL guitarra+bajo+batería. Mi menor heroico,
    //     progresión Em–C–G–D con bajo en CHUG (corcheas palm-mute) + armonía de quinta
    //     (power-chord), lead de guitarra agudo y batería rockera con doble bombo. Energía de
    //     escalada: subir 100 pisos. ---
    static readonly int[] stA_l = { 76, 0, 74, 72, 71, 0, 72, 74, 72, 0, 71, 69, 67, 0, 69, 0 };
    static readonly int[] stB_l = { 79, 0, 77, 76, 74, 0, 76, 79, 76, 0, 74, 72, 71, 0, 72, 0 };
    static readonly int[] stC_l = { 83, 0, 81, 79, 78, 0, 79, 81, 83, 84, 86, 0, 83, 0, 79, 0 };
    static readonly int[] stD_l = { 77, 76, 74, 72, 71, 69, 67, 69, 71, 72, 74, 76, 74, 0, 71, 0 };
    static readonly int[] stA_b = { 40, 40, 40, 0, 36, 36, 36, 0, 43, 43, 43, 0, 38, 38, 38, 0 };
    static readonly int[] stC_b = { 36, 36, 36, 0, 43, 43, 43, 0, 38, 38, 38, 0, 40, 40, 40, 0 };
    static readonly int[] stA_h = { 47, 47, 47, 0, 43, 43, 43, 0, 50, 50, 50, 0, 45, 45, 45, 0 };
    static readonly int[] stC_h = { 43, 43, 43, 0, 50, 50, 50, 0, 45, 45, 45, 0, 47, 47, 47, 0 };
    static readonly int[] stDr = { Kick, Hat, Kick, Hat, Snare, Hat, Kick, Kick, Kick, Hat, Snare, Hat, Snare, Hat, Kick, Hat };
    static readonly int[] STD_LEAD = Seq(stA_l, stB_l, stA_l, stC_l, stD_l, stB_l, stC_l, stD_l);
    static readonly int[] STD_BASS = Seq(stA_b, stA_b, stA_b, stC_b, stC_b, stA_b, stC_b, stC_b);
    static readonly int[] STD_HARM = Seq(stA_h, stA_h, stA_h, stC_h, stC_h, stA_h, stC_h, stC_h);
    static readonly int[] STD_DRUMS = Seq(stDr, stDr, stDr, stDr, stDr, stDr, stDr, stDr);

    // --- Combate de la Torre del Estudio (ENEMIGOS): ROCK rápido y agresivo guitarra+bajo+
    //     batería. Re menor, bajo GALOPANTE (root-quinta), progresión Dm–Bb–C–A (V mayor =
    //     tensión), lead frenético y batería veloz. ---
    static readonly int[] sbA_l = { 74, 0, 77, 74, 72, 0, 74, 77, 76, 0, 74, 72, 70, 0, 72, 0 };
    static readonly int[] sbB_l = { 81, 0, 79, 77, 76, 0, 77, 79, 77, 0, 76, 74, 72, 0, 74, 0 };
    static readonly int[] sbC_l = { 86, 0, 84, 81, 79, 0, 81, 84, 86, 0, 84, 81, 79, 77, 74, 0 };
    static readonly int[] sbD_l = { 77, 76, 74, 72, 70, 69, 67, 69, 70, 72, 74, 77, 76, 0, 74, 0 };
    static readonly int[] sbA_b = { 38, 45, 38, 45, 34, 41, 34, 41, 36, 43, 36, 43, 33, 40, 33, 40 };
    static readonly int[] sbB_b = { 38, 45, 38, 45, 34, 41, 34, 41, 29, 36, 29, 36, 36, 43, 36, 43 };
    static readonly int[] sbA_h = { 69, 0, 72, 0, 65, 0, 69, 0, 67, 0, 72, 0, 64, 0, 69, 0 };
    static readonly int[] sbB_h = { 69, 0, 74, 0, 65, 0, 70, 0, 60, 0, 65, 0, 64, 0, 67, 0 };
    static readonly int[] sbC_h = { 74, 0, 77, 0, 69, 0, 72, 0, 74, 0, 77, 0, 69, 0, 72, 0 };
    static readonly int[] sbD_h = { 72, 0, 69, 0, 65, 0, 62, 0, 65, 0, 69, 0, 64, 0, 67, 0 };
    static readonly int[] sbDr = { Kick, Hat, Snare, Hat, Kick, Kick, Snare, Hat, Kick, Hat, Snare, Hat, Snare, Kick, Snare, Hat };
    static readonly int[] STB_LEAD = Seq(sbA_l, sbB_l, sbA_l, sbC_l, sbD_l, sbB_l, sbC_l, sbD_l);
    static readonly int[] STB_BASS = Seq(sbA_b, sbB_b, sbA_b, sbA_b, sbB_b, sbB_b, sbA_b, sbB_b);
    static readonly int[] STB_HARM = Seq(sbA_h, sbB_h, sbA_h, sbC_h, sbD_h, sbB_h, sbC_h, sbD_h);
    static readonly int[] STB_DRUMS = Seq(sbDr, sbDr, sbDr, sbDr, sbDr, sbDr, sbDr, sbDr);

    // ================= PL-400: melodía propia por MUNDO (overworld + combate) =================
    // Mundos 1-3 reutilizan las pistas d1/d2/d3 y el combate bat1 (=BAT). Mundos 4-6 tienen
    // overworld propio (w4/w5/w6) y cada mundo su combate (bat1..bat6). En el MUNDO DEL EXAMEN
    // (torre final) todo (overworld y combate) suena al tema ÉPICO `exam`, que retoma la melodía
    // del TÍTULO ("la melodía del juego") con orquestación de batalla: todo se junta al final.

    // --- w4 Cripta del Servidor (ámbar/rojo): Do menor lento y ominoso, órgano grave ---
    static readonly int[] w4A_l = { 60, 0, 63, 65, 63, 0, 62, 60, 58, 0, 60, 62, 60, 0, 0, 0 };
    static readonly int[] w4B_l = { 63, 0, 65, 63, 62, 0, 60, 58, 55, 0, 58, 60, 58, 0, 0, 0 };
    static readonly int[] w4C_l = { 67, 0, 68, 67, 65, 63, 62, 60, 62, 63, 65, 63, 62, 0, 60, 0 };
    static readonly int[] w4A_b = { 36, 0, 43, 0, 36, 0, 43, 0, 32, 0, 39, 0, 36, 0, 0, 0 };
    static readonly int[] w4B_b = { 32, 0, 39, 0, 34, 0, 41, 0, 31, 0, 38, 0, 36, 0, 0, 0 };
    static readonly int[] w4C_b = { 41, 0, 48, 0, 43, 0, 39, 0, 44, 0, 39, 0, 36, 0, 36, 0 };
    static readonly int[] w4A_h = { 60, 63, 67, 63, 60, 63, 67, 63, 56, 60, 63, 60, 55, 58, 63, 0 };
    static readonly int[] w4B_h = { 63, 67, 70, 67, 62, 65, 68, 65, 55, 58, 63, 58, 55, 58, 63, 0 };
    static readonly int[] w4C_h = { 65, 68, 72, 68, 60, 63, 67, 63, 56, 60, 63, 60, 55, 60, 63, 0 };
    static readonly int[] W4_LEAD = Seq(w4A_l, w4B_l, w4A_l, w4C_l, w4A_l, w4B_l, w4C_l, w4A_l);
    static readonly int[] W4_BASS = Seq(w4A_b, w4B_b, w4A_b, w4C_b, w4A_b, w4B_b, w4C_b, w4A_b);
    static readonly int[] W4_HARM = Seq(w4A_h, w4B_h, w4A_h, w4C_h, w4A_h, w4B_h, w4C_h, w4A_h);
    static readonly int[] W4_DRUMS = Seq(d1Dr, d1Dr, d1Dr, d1Dr, d1Dr, d1Dr, d1Dr, d1Dr);

    // --- w5 Nexo de Azure (azul-cian eléctrico): Mi dórico brillante, arpegios, energético ---
    static readonly int[] w5A_l = { 64, 66, 67, 69, 71, 0, 69, 67, 66, 0, 67, 69, 67, 0, 64, 0 };
    static readonly int[] w5B_l = { 71, 0, 73, 74, 73, 71, 69, 67, 69, 0, 71, 69, 67, 66, 64, 0 };
    static readonly int[] w5C_l = { 76, 0, 74, 73, 71, 0, 69, 71, 73, 74, 76, 74, 73, 71, 69, 0 };
    static readonly int[] w5A_b = { 40, 47, 40, 47, 45, 52, 45, 52, 47, 54, 47, 54, 43, 50, 43, 50 };
    static readonly int[] w5B_b = { 45, 52, 45, 52, 43, 50, 43, 50, 40, 47, 40, 47, 47, 54, 47, 47 };
    static readonly int[] w5C_b = { 48, 55, 48, 55, 45, 52, 45, 52, 43, 50, 43, 50, 40, 47, 40, 40 };
    static readonly int[] w5A_h = { 64, 67, 71, 67, 64, 69, 72, 69, 62, 66, 69, 66, 67, 71, 74, 0 };
    static readonly int[] w5B_h = { 71, 74, 78, 74, 67, 71, 74, 71, 64, 67, 71, 67, 66, 69, 73, 0 };
    static readonly int[] w5C_h = { 72, 76, 79, 76, 69, 72, 76, 72, 67, 71, 74, 71, 64, 67, 71, 0 };
    static readonly int[] W5_LEAD = Seq(w5A_l, w5B_l, w5A_l, w5C_l, w5A_l, w5B_l, w5C_l, w5B_l);
    static readonly int[] W5_BASS = Seq(w5A_b, w5B_b, w5A_b, w5C_b, w5A_b, w5B_b, w5C_b, w5B_b);
    static readonly int[] W5_HARM = Seq(w5A_h, w5B_h, w5A_h, w5C_h, w5A_h, w5B_h, w5C_h, w5B_h);
    static readonly int[] W5_DRUMS = Seq(d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr);

    // --- w6 Ciudadela de Conectores y ALM (púrpura-dorado): Re menor con marcha regia, metales ---
    static readonly int[] w6A_l = { 62, 0, 65, 0, 69, 0, 65, 62, 64, 0, 65, 64, 62, 0, 0, 0 };
    static readonly int[] w6B_l = { 69, 0, 70, 69, 67, 0, 65, 64, 65, 0, 67, 65, 64, 0, 62, 0 };
    static readonly int[] w6C_l = { 74, 0, 73, 74, 70, 0, 69, 65, 67, 69, 70, 69, 67, 65, 62, 0 };
    static readonly int[] w6A_b = { 38, 0, 45, 0, 38, 0, 45, 0, 43, 0, 50, 0, 45, 0, 0, 0 };
    static readonly int[] w6B_b = { 41, 0, 48, 0, 43, 0, 50, 0, 41, 0, 48, 0, 45, 0, 45, 0 };
    static readonly int[] w6C_b = { 46, 0, 41, 0, 43, 0, 45, 0, 41, 0, 43, 0, 38, 0, 38, 0 };
    static readonly int[] w6A_h = { 62, 65, 69, 65, 62, 65, 69, 65, 59, 62, 67, 62, 57, 62, 66, 0 };
    static readonly int[] w6B_h = { 65, 69, 72, 69, 67, 70, 74, 70, 65, 69, 72, 69, 62, 66, 69, 0 };
    static readonly int[] w6C_h = { 70, 74, 77, 74, 65, 69, 72, 69, 67, 70, 74, 70, 62, 66, 69, 0 };
    static readonly int[] W6_LEAD = Seq(w6A_l, w6B_l, w6A_l, w6C_l, w6A_l, w6B_l, w6C_l, w6C_l);
    static readonly int[] W6_BASS = Seq(w6A_b, w6B_b, w6A_b, w6C_b, w6A_b, w6B_b, w6C_b, w6C_b);
    static readonly int[] W6_HARM = Seq(w6A_h, w6B_h, w6A_h, w6C_h, w6A_h, w6B_h, w6C_h, w6C_h);
    static readonly int[] W6_DRUMS = Seq(fnDr, fnDr, fnDr, fnDr, fnDr, fnDr, fnDr, fnDr);

    // --- Combate por mundo (bat1 = BAT reutilizado para el mundo 1) ---
    // bat2 Mundo 2 (verde): Mi frigio agresivo
    static readonly int[] b2A_l = { 76, 0, 77, 76, 72, 0, 71, 72, 74, 0, 72, 71, 69, 0, 67, 0 };
    static readonly int[] b2B_l = { 79, 0, 77, 76, 74, 0, 72, 71, 72, 0, 74, 72, 71, 69, 67, 0 };
    static readonly int[] b2C_l = { 81, 0, 80, 79, 77, 0, 76, 74, 72, 74, 76, 77, 76, 74, 72, 0 };
    static readonly int[] b2A_b = { 40, 47, 40, 47, 41, 48, 41, 48, 43, 50, 43, 50, 40, 47, 40, 47 };
    static readonly int[] b2B_b = { 45, 52, 45, 52, 43, 50, 43, 50, 41, 48, 41, 48, 40, 47, 40, 40 };
    static readonly int[] b2C_b = { 48, 55, 48, 55, 41, 48, 41, 48, 43, 50, 43, 50, 40, 47, 40, 0 };
    static readonly int[] b2A_h = { 71, 0, 76, 0, 72, 0, 77, 0, 71, 0, 74, 0, 67, 0, 71, 0 };
    static readonly int[] b2B_h = { 74, 0, 79, 0, 71, 0, 76, 0, 67, 0, 72, 0, 64, 0, 67, 0 };
    static readonly int[] b2C_h = { 76, 0, 81, 0, 72, 0, 77, 0, 74, 0, 79, 0, 67, 0, 71, 0 };
    static readonly int[] B2_LEAD = Seq(b2A_l, b2B_l, b2A_l, b2C_l, b2A_l, b2B_l, b2C_l, b2B_l);
    static readonly int[] B2_BASS = Seq(b2A_b, b2B_b, b2A_b, b2C_b, b2A_b, b2B_b, b2C_b, b2B_b);
    static readonly int[] B2_HARM = Seq(b2A_h, b2B_h, b2A_h, b2C_h, b2A_h, b2B_h, b2C_h, b2B_h);
    static readonly int[] B2_DRUMS = Seq(bDr, bDr, bDr, bDr, bDr, bDr, bDr, bDr);

    // bat3 Mundo 3 (violeta): Re menor mecánico veloz
    static readonly int[] b3A_l = { 74, 0, 77, 74, 72, 0, 74, 77, 76, 0, 74, 72, 70, 0, 72, 0 };
    static readonly int[] b3B_l = { 81, 0, 79, 77, 76, 0, 77, 79, 77, 0, 76, 74, 72, 0, 74, 0 };
    static readonly int[] b3C_l = { 86, 0, 84, 81, 79, 0, 81, 84, 86, 0, 84, 81, 79, 77, 74, 0 };
    static readonly int[] b3A_b = { 38, 45, 38, 45, 36, 43, 36, 43, 41, 48, 41, 48, 40, 47, 40, 47 };
    static readonly int[] b3B_b = { 41, 48, 41, 48, 43, 50, 43, 50, 38, 45, 38, 45, 40, 47, 40, 40 };
    static readonly int[] b3C_b = { 46, 53, 46, 53, 41, 48, 41, 48, 43, 50, 43, 50, 38, 45, 38, 0 };
    static readonly int[] b3A_h = { 69, 0, 74, 0, 65, 0, 69, 0, 72, 0, 77, 0, 64, 0, 69, 0 };
    static readonly int[] b3B_h = { 72, 0, 77, 0, 74, 0, 79, 0, 69, 0, 74, 0, 64, 0, 67, 0 };
    static readonly int[] b3C_h = { 77, 0, 81, 0, 72, 0, 77, 0, 74, 0, 79, 0, 64, 0, 69, 0 };
    static readonly int[] B3_LEAD = Seq(b3A_l, b3B_l, b3A_l, b3C_l, b3A_l, b3B_l, b3C_l, b3C_l);
    static readonly int[] B3_BASS = Seq(b3A_b, b3B_b, b3A_b, b3C_b, b3A_b, b3B_b, b3C_b, b3C_b);
    static readonly int[] B3_HARM = Seq(b3A_h, b3B_h, b3A_h, b3C_h, b3A_h, b3B_h, b3C_h, b3C_h);
    static readonly int[] B3_DRUMS = Seq(boDr, boDr, boDr, boDr, boDr, boDr, boDr, boDr);

    // bat4 Mundo 4 (ámbar): Do menor oscuro
    static readonly int[] b4A_l = { 72, 0, 75, 72, 70, 0, 72, 75, 74, 0, 72, 70, 68, 0, 70, 0 };
    static readonly int[] b4B_l = { 79, 0, 77, 75, 74, 0, 75, 77, 75, 0, 74, 72, 70, 0, 72, 0 };
    static readonly int[] b4C_l = { 84, 0, 82, 79, 77, 0, 79, 82, 84, 0, 82, 79, 77, 75, 72, 0 };
    static readonly int[] b4A_b = { 36, 43, 36, 43, 32, 39, 32, 39, 41, 48, 41, 48, 36, 43, 36, 43 };
    static readonly int[] b4B_b = { 39, 46, 39, 46, 41, 48, 41, 48, 36, 43, 36, 43, 32, 39, 32, 32 };
    static readonly int[] b4C_b = { 44, 51, 44, 51, 39, 46, 39, 46, 41, 48, 41, 48, 36, 43, 36, 0 };
    static readonly int[] b4A_h = { 67, 0, 72, 0, 63, 0, 68, 0, 65, 0, 70, 0, 60, 0, 63, 0 };
    static readonly int[] b4B_h = { 70, 0, 75, 0, 67, 0, 72, 0, 63, 0, 68, 0, 60, 0, 67, 0 };
    static readonly int[] b4C_h = { 75, 0, 79, 0, 67, 0, 72, 0, 68, 0, 72, 0, 60, 0, 63, 0 };
    static readonly int[] B4_LEAD = Seq(b4A_l, b4B_l, b4A_l, b4C_l, b4A_l, b4B_l, b4C_l, b4B_l);
    static readonly int[] B4_BASS = Seq(b4A_b, b4B_b, b4A_b, b4C_b, b4A_b, b4B_b, b4C_b, b4B_b);
    static readonly int[] B4_HARM = Seq(b4A_h, b4B_h, b4A_h, b4C_h, b4A_h, b4B_h, b4C_h, b4B_h);
    static readonly int[] B4_DRUMS = Seq(bDr, bDr, bDr, bDr, bDr, bDr, bDr, bDr);

    // bat5 Mundo 5 (azul eléctrico): Mi menor rápido y brillante
    static readonly int[] b5A_l = { 76, 0, 79, 76, 74, 0, 76, 79, 78, 0, 76, 74, 71, 0, 74, 0 };
    static readonly int[] b5B_l = { 83, 0, 81, 79, 76, 0, 78, 79, 79, 0, 78, 76, 74, 0, 71, 0 };
    static readonly int[] b5C_l = { 88, 0, 86, 83, 81, 0, 83, 86, 88, 0, 86, 83, 81, 79, 76, 0 };
    static readonly int[] b5A_b = { 40, 47, 40, 47, 36, 43, 36, 43, 45, 52, 45, 52, 43, 50, 43, 50 };
    static readonly int[] b5B_b = { 45, 52, 45, 52, 43, 50, 43, 50, 40, 47, 40, 47, 43, 50, 43, 43 };
    static readonly int[] b5C_b = { 48, 55, 48, 55, 45, 52, 45, 52, 43, 50, 43, 50, 40, 47, 40, 0 };
    static readonly int[] b5A_h = { 71, 0, 76, 0, 67, 0, 71, 0, 74, 0, 78, 0, 66, 0, 71, 0 };
    static readonly int[] b5B_h = { 74, 0, 79, 0, 71, 0, 76, 0, 67, 0, 71, 0, 66, 0, 69, 0 };
    static readonly int[] b5C_h = { 79, 0, 83, 0, 74, 0, 79, 0, 76, 0, 81, 0, 66, 0, 71, 0 };
    static readonly int[] B5_LEAD = Seq(b5A_l, b5B_l, b5A_l, b5C_l, b5A_l, b5B_l, b5C_l, b5B_l);
    static readonly int[] B5_BASS = Seq(b5A_b, b5B_b, b5A_b, b5C_b, b5A_b, b5B_b, b5C_b, b5B_b);
    static readonly int[] B5_HARM = Seq(b5A_h, b5B_h, b5A_h, b5C_h, b5A_h, b5B_h, b5C_h, b5B_h);
    static readonly int[] B5_DRUMS = Seq(d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr, d3Dr);

    // bat6 Mundo 6 (púrpura-dorado): Re menor regio y feroz
    static readonly int[] b6A_l = { 74, 0, 74, 77, 76, 0, 74, 72, 69, 0, 70, 69, 74, 0, 0, 0 };
    static readonly int[] b6B_l = { 81, 0, 80, 81, 77, 0, 76, 74, 72, 0, 74, 72, 69, 0, 74, 0 };
    static readonly int[] b6C_l = { 86, 0, 84, 81, 82, 0, 81, 77, 79, 81, 82, 81, 77, 74, 74, 0 };
    static readonly int[] b6A_b = { 38, 45, 38, 45, 41, 48, 41, 48, 43, 50, 43, 50, 38, 45, 38, 45 };
    static readonly int[] b6B_b = { 41, 48, 41, 48, 43, 50, 43, 50, 46, 53, 46, 53, 45, 52, 45, 45 };
    static readonly int[] b6C_b = { 46, 53, 46, 53, 41, 48, 41, 48, 43, 50, 43, 50, 38, 45, 38, 0 };
    static readonly int[] b6A_h = { 69, 0, 74, 0, 65, 0, 69, 0, 66, 0, 69, 0, 62, 0, 69, 0 };
    static readonly int[] b6B_h = { 72, 0, 77, 0, 69, 0, 74, 0, 70, 0, 74, 0, 69, 0, 72, 0 };
    static readonly int[] b6C_h = { 77, 0, 82, 0, 72, 0, 77, 0, 74, 0, 79, 0, 66, 0, 69, 0 };
    static readonly int[] B6_LEAD = Seq(b6A_l, b6B_l, b6A_l, b6C_l, b6A_l, b6B_l, b6C_l, b6C_l);
    static readonly int[] B6_BASS = Seq(b6A_b, b6B_b, b6A_b, b6C_b, b6A_b, b6B_b, b6C_b, b6C_b);
    static readonly int[] B6_HARM = Seq(b6A_h, b6B_h, b6A_h, b6C_h, b6A_h, b6B_h, b6C_h, b6C_h);
    static readonly int[] B6_DRUMS = Seq(boDr, boDr, boDr, boDr, boDr, boDr, boDr, boDr);

    // --- EXAMEN (torre final): la MELODÍA DEL JUEGO (himno del título) vuelve ÉPICA. Overworld
    //     y TODO combate del examen la comparten: aquí "se juntan" los mundos. Lead = motivos del
    //     título (tA/tB/tC_l) + armonía del título; bajo galopante y batería épica con platillos. ---
    static readonly int[] exA_b = { 36, 43, 48, 43, 55, 50, 43, 50, 36, 43, 48, 43, 55, 50, 43, 0 };
    static readonly int[] exB_b = { 45, 52, 57, 52, 41, 48, 53, 48, 45, 52, 57, 52, 41, 48, 53, 0 };
    static readonly int[] exC_b = { 41, 48, 53, 48, 43, 50, 55, 50, 36, 43, 48, 43, 36, 48, 36, 0 };
    static readonly int[] exDr = { Crash, 0, Kick, Hat, Snare, 0, Kick, Hat, Kick, Kick, Snare, Hat, Kick, Snare, Crash, Hat };
    static readonly int[] EXAM_LEAD = Seq(tA_l, tB_l, tA_l, tC_l, tA_l, tB_l, tC_l, tC_l);
    static readonly int[] EXAM_BASS = Seq(exA_b, exB_b, exA_b, exC_b, exA_b, exB_b, exC_b, exC_b);
    static readonly int[] EXAM_HARM = Seq(tA_h, tB_h, tA_h, tC_h, tA_h, tB_h, tC_h, tC_h);
    static readonly int[] EXAM_DRUMS = Seq(exDr, exDr, exDr, exDr, exDr, exDr, exDr, exDr);

    enum WaveShape { Pulse, Triangle, Sine }

    // SFX del overworld/acciones (sintetizados por código, cada uno con su carácter).
    AudioClip sfxPortal, sfxElevator, sfxCarriage, sfxBreak, sfxPotion, sfxJump, sfxKey, sfxPage, sfxClimb;
    AudioClip[] sfxSkills;

    void SetupAudio()
    {
        if (FindObjectOfType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
        musicSrc = gameObject.AddComponent<AudioSource>();
        musicSrc.loop = true; musicSrc.playOnAwake = false; musicSrc.volume = 0.58f;
        sfxSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc.loop = false; sfxSrc.playOnAwake = false; sfxSrc.volume = 0.55f;
        sfxRight = BuildMusic("sfxRight", new[] { 72, 79, 84 }, null, new[] { 76, 83, 88 }, null, 0.06f);
        sfxWrong = BuildMusic("sfxWrong", new[] { 53, 50, 48 }, null, null, null, 0.11f);
        sfxWin = BuildMusic("sfxWin", new[] { 72, 76, 79, 84, 88, 91 }, new[] { 48, 52, 55, 60, 64, 67 }, null, new[] { Crash, 0, Hat, 0, Snare, Crash }, 0.09f);

        // Portal: destello mágico que ASCIENDE (lead + armonía brillante).
        sfxPortal = BuildMusic("sfxPortal", new[] { 72, 76, 79, 84, 88, 91 }, null, new[] { 84, 88, 91, 96, 100, 103 }, null, 0.05f, WaveShape.Sine);
        // Ascensor: zumbido mecánico que SUBE (bajo grave en triángulo + hum).
        sfxElevator = BuildMusic("sfxElevator", new[] { 48, 50, 52, 55, 57, 60 }, new[] { 36, 38, 40, 43, 45, 48 }, null, null, 0.06f, WaveShape.Triangle);
        // Carruaje: trote (cascos de percusión + clip-clop del bajo).
        sfxCarriage = BuildMusic("sfxCarriage", new[] { 72, 0, 76, 0, 79, 0, 76, 0 }, new[] { 43, 0, 43, 0, 45, 0, 45, 0 }, null, new[] { Snare, 0, Hat, 0, Snare, 0, Hat, 0 }, 0.085f);
        // Romper muro: golpe/derrumbe (percusión grave que DESCIENDE).
        sfxBreak = BuildMusic("sfxBreak", new[] { 36, 31, 28 }, new[] { 30, 26, 24 }, null, new[] { Crash, Snare, Crash }, 0.07f, WaveShape.Triangle);
        // Poción: tintineo curativo suave que asciende.
        sfxPotion = BuildMusic("sfxPotion", new[] { 79, 83, 86, 91 }, null, new[] { 72, 76, 79, 84 }, null, 0.06f, WaveShape.Sine);
        // Salto de muro: "boing" rápido (sube y baja).
        sfxJump = BuildMusic("sfxJump", new[] { 67, 79, 72 }, null, null, null, 0.05f, WaveShape.Triangle);
        // Llave: chasquido de cerradura.
        sfxKey = BuildMusic("sfxKey", new[] { 84, 0, 88, 84 }, null, null, null, 0.05f);
        // Pasar página: "fwip" muy suave.
        sfxPage = BuildMusic("sfxPage", new[] { 88, 84 }, null, null, null, 0.04f, WaveShape.Sine);
        // Subir escalera (Torre del Estudio): PASOS sobre piedra que ASCIENDEN — bajo en
        // triángulo subiendo escalón a escalón + percusión de pisada, remate en platillo.
        sfxClimb = BuildMusic("sfxClimb",
            new[] { 50, 0, 52, 0, 53, 0, 55, 0, 57, 60 },
            new[] { 38, 0, 40, 0, 41, 0, 43, 0, 45, 48 },
            null,
            new[] { Snare, 0, Hat, 0, Snare, 0, Hat, 0, Snare, Crash },
            0.075f, WaveShape.Triangle);

        // Skills: una firma sonora por habilidad, escalando con el nivel (idx+2 notas/épica).
        sfxSkills = new[]
        {
            BuildMusic("sk0", new[] { 83, 88 }, null, null, null, 0.06f),                                                   // Doble Filo ×2
            BuildMusic("sk1", new[] { 79, 83, 88 }, null, new[] { 67, 71, 76 }, null, 0.06f),                               // Tríada Rúnica ×3
            BuildMusic("sk2", new[] { 76, 79, 83, 88 }, null, null, new[] { 0, Hat, 0, Crash }, 0.06f),                     // Tormenta Tetra ×4
            BuildMusic("sk3", new[] { 72, 76, 79, 83, 88 }, new[] { 48, 52, 55, 60, 64 }, null, null, 0.055f),              // Quinta Sentencia ×5
            BuildMusic("sk4", new[] { 72, 76, 79, 83, 88, 91 }, new[] { 36, 43, 48, 55, 60, 67 }, null, new[] { Crash, 0, Hat, 0, Snare, Crash }, 0.06f), // Hexágono Astral ×6
        };
    }

    // Sonido por tipo de animación del overworld/acción (hub PlayCutIn).
    void PlaySfxForCutIn(string baseKey)
    {
        switch (baseKey)
        {
            case "portal": PlaySfx(sfxPortal); break;
            case "elevup": PlaySfx(sfxElevator); break;
            case "climb": PlaySfx(sfxClimb); break;   // pasos subiendo la escalera de la Torre del Estudio
            case "ride": PlaySfx(sfxCarriage); break;
            case "break": PlaySfx(sfxBreak); break;
            case "potion": PlaySfx(sfxPotion); break;
            case "jump": PlaySfx(sfxJump); break;
            case "usekey": PlaySfx(sfxKey); break;
            case "page": PlaySfx(sfxPage); break;
        }
    }

    // Firma sonora de cada habilidad de combate.
    void PlaySkillSfx(int idx)
    {
        if (sfxSkills != null && sfxSkills.Length > 0)
            PlaySfx(sfxSkills[Mathf.Clamp(idx, 0, sfxSkills.Length - 1)]);
    }

    void PlayTrack(string name)
    {
        if (musicSrc == null || currentTrack == name) return;
        currentTrack = name;
        AudioClip c;
        if (!musicClips.TryGetValue(name, out c)) { c = BuildTrack(name); musicClips[name] = c; }
        musicSrc.clip = c; musicSrc.loop = true; musicSrc.Play();
    }

    AudioClip BuildTrack(string name)
    {
        switch (name)
        {
            case "title": return BuildMusic("title", TITLE_LEAD, TITLE_BASS, TITLE_HARM, TITLE_DRUMS, 0.16f);
            // Escalada de miedo/desafío: d1 inquietante > d2 tétrica > d3 industrial > torre final.
            case "d1": return BuildMusic("d1", D1_LEAD, D1_BASS, D1_HARM, D1_DRUMS, 0.19f);
            case "d2": return BuildMusic("d2", D2_LEAD, D2_BASS, D2_HARM, D2_DRUMS, 0.165f);
            case "d3": return BuildMusic("d3", D3_LEAD, D3_BASS, D3_HARM, D3_DRUMS, 0.145f);
            case "final": return BuildMusic("final", FIN_LEAD, FIN_BASS, FIN_HARM, FIN_DRUMS, 0.13f);
            case "battle": return BuildMusic("battle", BAT_LEAD, BAT_BASS, BAT_HARM, BAT_DRUMS, 0.115f);
            // Overworld propio de los mundos 4-6 (1-3 reutilizan d1/d2/d3).
            case "w4": return BuildMusic("w4", W4_LEAD, W4_BASS, W4_HARM, W4_DRUMS, 0.175f);
            case "w5": return BuildMusic("w5", W5_LEAD, W5_BASS, W5_HARM, W5_DRUMS, 0.15f);
            case "w6": return BuildMusic("w6", W6_LEAD, W6_BASS, W6_HARM, W6_DRUMS, 0.16f);
            // Combate propio por mundo (bat1 reutiliza la pista de combate estándar).
            case "bat1": return BuildMusic("bat1", BAT_LEAD, BAT_BASS, BAT_HARM, BAT_DRUMS, 0.115f);
            case "bat2": return BuildMusic("bat2", B2_LEAD, B2_BASS, B2_HARM, B2_DRUMS, 0.11f);
            case "bat3": return BuildMusic("bat3", B3_LEAD, B3_BASS, B3_HARM, B3_DRUMS, 0.108f);
            case "bat4": return BuildMusic("bat4", B4_LEAD, B4_BASS, B4_HARM, B4_DRUMS, 0.112f);
            case "bat5": return BuildMusic("bat5", B5_LEAD, B5_BASS, B5_HARM, B5_DRUMS, 0.105f);
            case "bat6": return BuildMusic("bat6", B6_LEAD, B6_BASS, B6_HARM, B6_DRUMS, 0.11f);
            // Examen (torre final): la melodía del juego, épica. Overworld y combate la comparten.
            case "exam": return BuildMusic("exam", EXAM_LEAD, EXAM_BASS, EXAM_HARM, EXAM_DRUMS, 0.13f);
            case "boss": return BuildMusic("boss", BOSS_LEAD, BOSS_BASS, BOSS_HARM, BOSS_DRUMS, 0.125f);
            case "sage": return BuildMusic("sage", SAGE_LEAD, SAGE_BASS, SAGE_HARM, SAGE_DRUMS, 0.155f);
            // Lectura serena (piano suave, sin batería): la comparten los tomos, los súper tomos
            // y la Granja de Monstruos.
            case "tome": return BuildMusic("tome", TOME_LEAD, TOME_BASS, TOME_HARM, null, 0.30f, WaveShape.Sine);
            // Gran Sabio: jefe final del piso 18, épico y aterrador (distinto del jefe normal).
            case "gransabio": return BuildMusic("gransabio", GS_LEAD, GS_BASS, GS_HARM, GS_DRUMS, 0.12f);
            // Torre del Gran Sabio (recorrido): METAL desafiante con guitarras (power-chords) y batería.
            case "tower": return BuildMusic("tower", TWR_LEAD, TWR_BASS, TWR_HARM, TWR_DRUMS, 0.115f);
            // Torre del Estudio (recorrido): ROCK heroico de guitarra+bajo+batería (escalada de 100 pisos).
            case "studytower": return BuildMusic("studytower", STD_LEAD, STD_BASS, STD_HARM, STD_DRUMS, 0.12f);
            // Combate de la Torre del Estudio: ROCK rápido y agresivo (enemigos de cualquier tipo).
            case "studybattle": return BuildMusic("studybattle", STB_LEAD, STB_BASS, STB_HARM, STB_DRUMS, 0.105f);
            default: return BuildMusic("town", TOWN_LEAD, TOWN_BASS, TOWN_HARM, TOWN_DRUMS, 0.17f);
        }
    }

    AudioClip BuildMusic(string name, int[] lead, int[] bass, int[] harmony, int[] drums, float step, WaveShape leadShape = WaveShape.Pulse)
    {
        int sr = 22050;
        int stepN = Mathf.Max(1, Mathf.RoundToInt(step * sr));
        int len = lead != null ? lead.Length : 0;
        if (bass != null) len = Mathf.Max(len, bass.Length);
        if (harmony != null) len = Mathf.Max(len, harmony.Length);
        if (drums != null) len = Mathf.Max(len, drums.Length);
        var data = new float[stepN * len];
        for (int i = 0; i < len; i++)
        {
            if (lead != null && i < lead.Length) FillTone(data, i * stepN, stepN, lead[i], 0.24f, sr, leadShape);
            if (bass != null && i < bass.Length) FillTone(data, i * stepN, stepN, bass[i], 0.18f, sr, WaveShape.Triangle);
            if (harmony != null && i < harmony.Length) FillTone(data, i * stepN, stepN, harmony[i], 0.135f, sr, WaveShape.Sine);
            if (drums != null && i < drums.Length) FillDrum(data, i * stepN, stepN, drums[i], sr);
        }
        ApplyEcho(data, stepN * 3 / 2, 0.26f, len >= 32);   // sin envolver el eco en los SFX cortos
        SoftLimit(data);
        var clip = AudioClip.Create(name, data.Length, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    void FillTone(float[] data, int start, int n, int midi, float amp, int sr, WaveShape shape)
    {
        if (midi <= 0) return; // silencio
        float f = 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        float atk = sr * 0.006f, rel = Mathf.Min(n * 0.42f, sr * 0.05f);
        // Vibrato suave (solo melodía y armonía) con fase integrada: chiptune más cálido.
        float vibDepth = shape == WaveShape.Triangle ? 0f : 0.0035f;
        // La armonía (Sine) suena como ARPA: envolvente con caída tipo pulsación (pluck).
        float pluck = shape == WaveShape.Sine ? sr * 0.16f : 0f;
        float ph = 0f, ph2 = 0f;
        for (int s = 0; s < n; s++)
        {
            float env = Mathf.Min(1f, s / atk) * Mathf.Min(1f, (n - s) / rel);
            if (pluck > 0f) env *= Mathf.Exp(-s / pluck);
            float vib = 1f + vibDepth * Mathf.Sin(2f * Mathf.PI * 5.5f * s / sr);
            ph += f * vib / sr;
            float phase = ph - Mathf.Floor(ph);
            float sample;
            if (shape == WaveShape.Triangle) sample = 4f * Mathf.Abs(phase - 0.5f) - 1f;
            else if (shape == WaveShape.Sine) sample = Mathf.Sin(2f * Mathf.PI * phase);
            else
            {
                // Pulso 30% suavizado con un toque de triángulo (menos chillón que el cuadrado puro).
                float sq = phase < 0.30f ? 1f : -1f;
                float tri = 4f * Mathf.Abs(phase - 0.5f) - 1f;
                sample = sq * 0.72f + tri * 0.20f;
                // 2º armónico suave (octava arriba): brillo "metálico" de chip JRPG sin chillar.
                ph2 += 2f * f * vib / sr;
                float p2 = ph2 - Mathf.Floor(ph2);
                sample += (p2 < 0.5f ? 1f : -1f) * 0.08f;
            }
            data[start + s] += sample * amp * env;
        }
    }

    void FillDrum(float[] data, int start, int n, int drum, int sr)
    {
        if (drum <= 0) return;
        int len = drum == Crash ? n : Mathf.Min(n, sr / 10);
        for (int s = 0; s < len && start + s < data.Length; s++)
        {
            float t = s / (float)sr;
            float env = Mathf.Exp(-s / (float)(drum == Crash ? sr * 0.09f : sr * 0.035f));
            float noise = Mathf.Sin((start + s) * 12.9898f) * 43758.5453f;
            noise = (noise - Mathf.Floor(noise)) * 2f - 1f;
            float sample = 0f;
            if (drum == Kick) sample = Mathf.Sin(2f * Mathf.PI * (82f - 44f * t) * t) * env * 0.46f;
            else if (drum == Snare) sample = (noise * 0.30f + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.10f) * env;
            else if (drum == Hat) sample = noise * env * 0.12f;
            else if (drum == Crash) sample = noise * env * 0.20f;
            data[start + s] += sample;
        }
    }

    // Eco de un solo golpe que da amplitud y "salón" al chiptune. Envuelve el final
    // del clip hacia el principio para que el loop no corte la cola del eco.
    void ApplyEcho(float[] data, int delay, float gain, bool wrap)
    {
        if (delay <= 0 || data.Length <= delay) return;
        for (int i = delay; i < data.Length; i++) data[i] += data[i - delay] * gain;
        if (!wrap) return;
        for (int i = 0; i < delay; i++) data[i] += data[data.Length - delay + i] * gain * 0.7f;
    }

    void SoftLimit(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] = Mathf.Clamp(data[i] * 0.92f, -0.92f, 0.92f);
    }

    void PlaySfx(AudioClip c) { if (sfxSrc != null && c != null) sfxSrc.PlayOneShot(c); }
}
