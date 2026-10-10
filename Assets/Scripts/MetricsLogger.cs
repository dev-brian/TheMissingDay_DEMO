using System;
using System.IO;
using UnityEngine;

// Registra cada intento de desafío en la consola de Unity y en un CSV local.
// El CSV queda en Application.persistentDataPath/metrics.csv (también funciona en el .exe).
public static class MetricsLogger
{
    public static string StudentId = "ID_01";

    static string CsvPath => Path.Combine(Application.persistentDataPath, "metrics.csv");

    public static void Log(string challengeId, int attempts, bool correct, float seconds)
    {
        Debug.Log($"[METRIC LOG] Alumno: {StudentId} | Desafío: {challengeId} | Intentos: {attempts} | Acierto: {correct} | Tiempo: {seconds:0}s");

        try
        {
            bool isNew = !File.Exists(CsvPath);
            using (var w = new StreamWriter(CsvPath, true))
            {
                if (isNew) w.WriteLine("fecha,alumno,desafio,intentos,acierto,segundos");
                w.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{StudentId},{challengeId},{attempts},{correct},{seconds:0.0}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[METRIC LOG] No se pudo escribir el CSV: {e.Message}");
        }
    }

    public static void LogAnswer(string challengeId, int attempt, string answer, bool correct)
    {
        Debug.Log($"[METRIC LOG] Alumno: {StudentId} | Desafío: {challengeId} | Intento {attempt}: \"{answer}\" -> {(correct ? "correcto" : "incorrecto")}");
    }
}
