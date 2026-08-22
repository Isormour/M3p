using Match3;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    /// <summary>Builds Recycle and Transmute overlays on the highest sorting canvas.</summary>
    public static class SkillCastPromptUI
    {
        public static Transform FindCanvasParent()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Canvas best = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isActiveAndEnabled)
                    continue;

                if (best == null || canvas.sortingOrder >= best.sortingOrder)
                    best = canvas;
            }

            return best != null ? best.transform : null;
        }

        public static UICardChoiceOverlay ShowDiscardReturn(
            BattleDeck deck,
            Action<int> picked,
            Action cancelled)
        {
            Transform parent = FindCanvasParent();
            if (parent == null || deck == null || deck.DiscardPileCount == 0)
            {
                cancelled?.Invoke();
                return null;
            }

            var options = new List<CardChoiceOption>(deck.DiscardPileCount);
            for (int i = 0; i < deck.DiscardPile.Count; i++)
            {
                BoardActionCardDefinition card = deck.DiscardPile[i];
                string label = card != null ? card.DisplayName : "?";
                options.Add(new CardChoiceOption(label, new Color(0.2f, 0.25f, 0.35f, 1f), i));
            }

            return UICardChoiceOverlay.Show(parent, "Karta", options, picked, cancelled);
        }

        public static UICardChoiceOverlay ShowManaColor(
            string title,
            SoftStats softStats,
            SkillDefinition skill,
            int excludeTypeId,
            bool remainingAfterCost,
            bool requirePositiveAmount,
            Action<int> picked,
            Action cancelled)
        {
            Transform parent = FindCanvasParent();
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (parent == null || config == null || softStats == null)
            {
                cancelled?.Invoke();
                return null;
            }

            var options = new List<CardChoiceOption>();
            for (int i = 0; i < config.TileTypeCount; i++)
            {
                if (i == excludeTypeId)
                    continue;

                Match3TileTypeDefinition tileType = config.GetTileType(i);
                if (tileType == null)
                    continue;

                int amount = softStats.GetManaForTileType(i);
                if (remainingAfterCost && skill != null)
                    amount -= skill.GetManaCostForTileType(tileType);

                if (requirePositiveAmount && amount <= 0)
                    continue;

                options.Add(new CardChoiceOption($"{tileType.name} {Mathf.Max(0, amount)}", tileType.Color, i));
            }

            if (options.Count == 0)
            {
                cancelled?.Invoke();
                return null;
            }

            return UICardChoiceOverlay.Show(parent, title, options, picked, cancelled);
        }
    }
}
