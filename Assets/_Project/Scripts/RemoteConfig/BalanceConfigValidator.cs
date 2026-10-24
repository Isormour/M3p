using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace M3P.RemoteConfig
{
    /// <summary>
    /// Contract checks shared by the editor and the player. A known bad entry rejects the whole applied set.
    /// </summary>
    public static class BalanceConfigValidator
    {
        public static void Validate(string typeId, ConfigContract contract, JObject data, List<string> errors)
        {
            if (errors == null)
                return;

            if (contract == null)
            {
                errors.Add("Brak kontraktu.");
                return;
            }

            if (data == null)
            {
                errors.Add("Brak danych.");
                return;
            }

            if (contract.fields != null)
            {
                for (int i = 0; i < contract.fields.Count; i++)
                {
                    ConfigField field = contract.fields[i];
                    if (field == null)
                        continue;

                    JToken value = data[field.name];
                    if (value == null)
                    {
                        errors.Add("Brak wymaganego pola " + field.name + ".");
                        continue;
                    }

                    if (!ConfigValueCodec.TryNormalize(value, field, out _, out string error))
                        errors.Add(field.name + ": " + error);
                }
            }

            if (typeId == M3pConfigTypes.Battle)
                ValidateBattle(data, errors);
            else if (typeId == M3pConfigTypes.LevelProgression)
                ValidateLevelProgression(data, errors);
        }

        public static void ValidateBattle(JObject data, List<string> errors)
        {
            JToken bonuses = data?["damagePerSupermatchSize"];
            if (bonuses == null || bonuses.Type != JTokenType.Array)
            {
                errors.Add("damagePerSupermatchSize musi być tablicą ośmiu bonusów.");
                return;
            }

            var array = (JArray)bonuses;
            int expected = BattleConfig.MaxSupermatchSize - BattleConfig.MinSupermatchSize + 1;
            if (array.Count != expected)
            {
                errors.Add("Tablica bonusów supermatcha ma mieć dokładnie "
                    + expected.ToString(CultureInfo.InvariantCulture) + " elementów.");
            }
        }

        public static void ValidateLevelProgression(JObject data, List<string> errors)
        {
            if (!ConfigValueCodec.TryReadInt(data?["levelCap"], out int levelCap, out _))
            {
                errors.Add("levelCap musi być liczbą całkowitą.");
                return;
            }

            if (!ConfigValueCodec.TryReadInt(data?["baseExperiencePerLevel"], out int baseExperience, out _))
            {
                errors.Add("baseExperiencePerLevel musi być liczbą całkowitą.");
                return;
            }

            if (!ConfigValueCodec.TryReadInt(data?["experienceIncreasePerLevel"], out int increase, out _))
            {
                errors.Add("experienceIncreasePerLevel musi być liczbą całkowitą.");
                return;
            }

            if (levelCap < LevelProgressionConfig.FirstLevel)
                errors.Add("levelCap musi być co najmniej 1.");
            if (baseExperience < 1)
                errors.Add("Wymagane doświadczenie poziomu musi być dodatnie.");
            if (increase < 0)
                errors.Add("Przyrost doświadczenia nie może być ujemny.");

            if (errors.Count > 0)
                return;

            long total = 0;
            for (int level = LevelProgressionConfig.FirstLevel; level < levelCap; level++)
            {
                long cost = (long)baseExperience + (long)increase * (level - LevelProgressionConfig.FirstLevel);
                if (cost <= 0 || cost > int.MaxValue)
                {
                    errors.Add("Koszt poziomu nie mieści się w obsługiwanym zakresie.");
                    return;
                }

                total += cost;
                if (total > int.MaxValue)
                {
                    errors.Add("Suma doświadczenia do limitu poziomu nie mieści się w obsługiwanym zakresie.");
                    return;
                }
            }
        }
    }
}
