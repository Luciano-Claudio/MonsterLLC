using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// D9.6a (docs/superpowers/specs/2026-11-22-d9-6ab-monstros-xp-nivel-design.md, 5.2/5.5) — aplica a
// tabela por tier (MonsterScalingSettings) nos prefabs do roster comum e dos mini bosses.
// - Cada monstro mantém a proporção que tem hoje na média do tier, por tipo (melee/ranged); mini
//   boss é medido contra os outros mini bosses do tier dele (o único do tier = ×4 vida / ×1,5 dano exatos).
// - Velocidade: média do tier (MonsterScaling.TierSpeed) × a proporção dele.
// - Campos float com "damage"/"heal" nos componentes do prefab (venenos, poças, ataques extras)
//   e nos prefabs que ele referencia (totens, filhos, hazards) escalam pela razão do dano.
// - Bosses NÃO são tocados (rework deles fica pro fim).
// Prévia primeiro (só relatório), depois aplicar. Menu: MonsterLLC/Escala dos monstros.
public static class MonsterScalingApplier
{
    private static readonly string[] ScaledWords = { "damage", "heal" };
    private static readonly string[] SkipWords = { "multiplier", "percent", "radius", "chance", "cooldown", "duration", "interval", "speed", "range", "delay", "time", "fraction", "factor", "step" };

    [MenuItem("MonsterLLC/Escala dos monstros/Prévia (só relatório)")]
    public static void PreviewMenu() => Debug.Log(Run(false));

    [MenuItem("MonsterLLC/Escala dos monstros/Aplicar nos prefabs")]
    public static void ApplyMenu() => Debug.Log(Run(true));

    private struct Entry
    {
        public EnemyDefinitionSO def;
        public GameObject prefab;
        public EnemyController enemy;
        public MonsterKind kind;
    }

    public static string Run(bool apply)
    {
        var settings = MonsterScalingSettings.Load();
        if (settings != null) settings.ApplyToRuntime();

        var spawn = AssetDatabase.LoadAssetAtPath<MonsterSpawnSettings>("Assets/Settings/MonsterSpawnSettings.asset");
        if (spawn == null) return "FAIL sem MonsterSpawnSettings";

        var entries = new List<Entry>();
        foreach (var def in spawn.roster) Add(entries, def, false);
        foreach (var def in spawn.miniBossRoster) Add(entries, def, true);

        // Médias atuais por (tier, tipo). Mini boss usa a média dos melee comuns.
        var sumHp = new Dictionary<(int, MonsterKind), float>();
        var sumDmg = new Dictionary<(int, MonsterKind), float>();
        var count = new Dictionary<(int, MonsterKind), int>();
        var sumSpeed = new Dictionary<int, float>();
        var countSpeed = new Dictionary<int, int>();
        foreach (var e in entries)
        {
            var key = (e.def.tier, e.kind); // mini boss tem o próprio grupo (ratio entre os mini bosses do tier)
            sumHp[key] = sumHp.GetValueOrDefault(key) + e.enemy.stats.maxHealth;
            sumDmg[key] = sumDmg.GetValueOrDefault(key) + e.enemy.stats.attackDamage;
            count[key] = count.GetValueOrDefault(key) + 1;
            if (e.kind == MonsterKind.MiniBoss) continue; // velocidade média do tier só pelos comuns
            sumSpeed[e.def.tier] = sumSpeed.GetValueOrDefault(e.def.tier) + e.enemy.stats.moveSpeed;
            countSpeed[e.def.tier] = countSpeed.GetValueOrDefault(e.def.tier) + 1;
        }

        var report = new StringBuilder(apply ? "PASS APLICADO\n" : "PASS PRÉVIA (nada salvo)\n");
        var scaledRefs = new Dictionary<GameObject, float>();
        var conflicts = new List<string>();

        foreach (var e in entries)
        {
            var key = (e.def.tier, e.kind);
            if (!count.TryGetValue(key, out int n) || n == 0) key = (e.def.tier, MonsterKind.Melee);
            if (!count.TryGetValue(key, out n) || n == 0) { report.Append($"  ! {e.def.name}: sem média no T{e.def.tier}, pulado\n"); continue; }

            float avgHp = sumHp[key] / n, avgDmg = sumDmg[key] / n;
            float avgSpeed = countSpeed.TryGetValue(e.def.tier, out int ns) && ns > 0 ? sumSpeed[e.def.tier] / ns : e.enemy.stats.moveSpeed;
            float oldHp = e.enemy.stats.maxHealth, oldDmg = e.enemy.stats.attackDamage, oldSpeed = e.enemy.stats.moveSpeed;
            float newHp = Mathf.Round(MonsterScaling.TargetHealth(e.def.tier, e.kind, avgHp > 0 ? oldHp / avgHp : 1f));
            float newDmg = Mathf.Max(1f, Mathf.Round(MonsterScaling.TargetDamage(e.def.tier, e.kind, avgDmg > 0 ? oldDmg / avgDmg : 1f)));
            float newSpeed = Mathf.Round(MonsterScaling.TierSpeed(e.def.tier) * (avgSpeed > 0 ? oldSpeed / avgSpeed : 1f) * 100f) / 100f;
            float dmgRatio = oldDmg > 0 ? newDmg / oldDmg : 1f;
            float hpRatio = oldHp > 0 ? newHp / oldHp : 1f;

            report.Append($"T{e.def.tier} {e.kind,-8} {e.prefab.name,-26} vida {oldHp:N0} → {newHp:N0} | dano {oldDmg:N0} → {newDmg:N0} | vel {oldSpeed} → {newSpeed}\n");

            if (apply)
            {
                var so = new SerializedObject(e.enemy);
                so.FindProperty("stats.maxHealth").floatValue = newHp;
                so.FindProperty("stats.health").floatValue = newHp;
                so.FindProperty("stats.attackDamage").floatValue = newDmg;
                so.FindProperty("stats.moveSpeed").floatValue = newSpeed;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(e.enemy);
            }

            ScaleExtraFields(e.prefab, e.enemy, dmgRatio, hpRatio, e.def.tier, apply, report, "    ");
            ScaleReferencedPrefabs(e.prefab, dmgRatio, hpRatio, e.def.tier, apply, report, scaledRefs, conflicts);
        }

        foreach (var c in conflicts) report.Append("  ! conflito: " + c + "\n");
        if (apply) AssetDatabase.SaveAssets();
        return report.ToString();
    }

    private static void Add(List<Entry> list, EnemyDefinitionSO def, bool mini)
    {
        if (def == null || def.prefab == null) return;
        var enemy = def.prefab.GetComponent<EnemyController>();
        if (enemy == null) return;
        var kind = mini ? MonsterKind.MiniBoss : def.isRanged ? MonsterKind.Ranged : MonsterKind.Melee;
        list.Add(new Entry { def = def, prefab = def.prefab, enemy = enemy, kind = kind });
    }

    // 0 = não escala; 1 = pela razão do dano do monstro; 2 = pela razão da vida; 3 = pela tabela
    // do tier (dano do tier ÷ T1). Os campos "por segundo/por tick" (venenos, poças, totens) eram
    // um valor fixo (2) em todos os tiers no sistema antigo — escalar pela razão do monstro os
    // zerava nos tiers altos; pela tabela, o 2 do T1 continua 2 e cresce com o tier.
    private static int FieldKind(string name)
    {
        string lower = name.ToLowerInvariant();
        if (lower.Contains("heal") && !lower.Contains("health"))
        {
            foreach (var w in SkipWords) if (lower.Contains(w)) return 0;
            return 2; // cura repõe vida -> pela razão da vida do monstro
        }
        if (lower.Contains("persecond") && lower.Contains("damage")) return 3; // veneno/poça: pela tabela do tier
        if (lower.Contains("pertick") && lower.Contains("damage")) return 1;   // tick de dano: pela razão do dano
        foreach (var w in SkipWords) if (lower.Contains(w)) return 0;
        if (lower.Contains("health")) return 2;
        foreach (var w in ScaledWords) if (lower.Contains(w)) return 1;
        return 0;
    }

    // Campos float de dano/cura dos componentes do prefab, fora o attackDamage dos stats (já feito).
    private static void ScaleExtraFields(GameObject prefab, Component skipStatsOf, float ratio, float hpRatio, int tier, bool apply, StringBuilder report, string indent)
    {
        float tierFactor = MonsterScaling.TargetDamage(tier, MonsterKind.Melee, 1f) / MonsterScaling.TargetDamage(1, MonsterKind.Melee, 1f);
        foreach (var comp in prefab.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null) continue;
            var so = new SerializedObject(comp);
            var it = so.GetIterator();
            bool changed = false;
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.Float) continue;
                if (comp == skipStatsOf && it.propertyPath.StartsWith("stats.")) continue;
                int kind = FieldKind(it.name);
                if (kind == 0) continue;
                float r = kind == 2 ? hpRatio : kind == 3 ? tierFactor : ratio;
                float old = it.floatValue;
                if (old == 0f || Mathf.Approximately(r, 1f)) continue;
                float nv = Mathf.Max(0.1f, Mathf.Round(old * r * 10f) / 10f);
                report.Append($"{indent}{comp.GetType().Name}.{it.propertyPath}: {old} → {nv}\n");
                if (apply) { it.floatValue = nv; changed = true; }
            }
            if (changed) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(comp); }
        }
    }

    // Prefabs referenciados pelo monstro (totens, filhos, hazards). Um prefab só é escalado uma
    // vez; se outro monstro o referencia com outra razão, vira conflito no relatório.
    private static void ScaleReferencedPrefabs(GameObject prefab, float dmgRatio, float hpRatio, int tier, bool apply,
        StringBuilder report, Dictionary<GameObject, float> scaledRefs, List<string> conflicts)
    {
        foreach (var comp in prefab.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null) continue;
            var it = new SerializedObject(comp).GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                var target = it.objectReferenceValue as GameObject;
                if (target != null) target = target.transform.root.gameObject; // filho de prefab -> raiz (nunca escala o mesmo prefab 2x)
                if (target == null || !EditorUtility.IsPersistent(target) || target == prefab) continue;
                if (target.GetComponentInChildren<MonoBehaviour>(true) == null) continue;
                if (scaledRefs.TryGetValue(target, out float used))
                {
                    if (!float.IsNaN(used) && !Mathf.Approximately(used, dmgRatio)) conflicts.Add($"{target.name} usado por mais de um monstro (razões {used:0.###} e {dmgRatio:0.###}); ficou com a primeira");
                    continue;
                }
                scaledRefs[target] = dmgRatio;
                report.Append($"    → {target.name} (referenciado por {prefab.name})\n");

                var childEnemy = target.GetComponent<EnemyController>();
                if (childEnemy != null)
                {
                    float hp = Mathf.Round(childEnemy.stats.maxHealth * hpRatio);
                    float dmg = Mathf.Max(1f, Mathf.Round(childEnemy.stats.attackDamage * dmgRatio));
                    report.Append($"      {target.name} stats: vida {childEnemy.stats.maxHealth} → {hp} | dano {childEnemy.stats.attackDamage} → {dmg}\n");
                    if (apply)
                    {
                        var so = new SerializedObject(childEnemy);
                        so.FindProperty("stats.maxHealth").floatValue = hp;
                        so.FindProperty("stats.health").floatValue = hp;
                        so.FindProperty("stats.attackDamage").floatValue = dmg;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(childEnemy);
                    }
                }
                ScaleExtraFields(target, childEnemy, dmgRatio, hpRatio, tier, apply, report, "      ");
            }
        }
    }
}
