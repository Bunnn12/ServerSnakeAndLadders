using SnakeAndLadders.Contracts.Dtos.Gameplay;
using SnakeAndLadders.Contracts.Enums;
using SnakesAndLadders.Services.Logic.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakesAndLadders.Services.Wcf.Gameplay
{
    internal static class GameplayResponseBuilder
    {
        private const string EFFECT_TOKEN_LADDER = "LADDER";
        private const string EFFECT_TOKEN_SNAKE = "SNAKE";

        public static RollDiceResponseDto BuildRollDiceResponse(
            RollDiceRequestDto request,
            RollDiceResult moveResult)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (moveResult == null)
            {
                throw new ArgumentNullException(nameof(moveResult));
            }

            MoveEffectType effectType = MapMoveEffectType(moveResult.ExtraInfo);

            return new RollDiceResponseDto
            {
                Success = true,
                FailureReason = null,
                PlayerUserId = request.PlayerUserId,
                FromCellIndex = moveResult.FromCellIndex,
                ToCellIndex = moveResult.ToCellIndex,
                DiceValue = moveResult.DiceValue,
                MoveResult = effectType,
                ExtraInfo = moveResult.ExtraInfo,
                MessageIndex = moveResult.MessageIndex,
                UpdatedTokens = null,
                GrantedItemCode = moveResult.GrantedItemCode,
                GrantedDiceCode = moveResult.GrantedDiceCode
            };
        }

        public static PlayerMoveResultDto BuildPlayerMoveResultDto(
            int userId,
            RollDiceResult moveResult)
        {
            if (moveResult == null)
            {
                throw new ArgumentNullException(nameof(moveResult));
            }

            MoveEffectType effectType = MapMoveEffectType(moveResult.ExtraInfo);

            return new PlayerMoveResultDto
            {
                UserId = userId,
                FromCellIndex = moveResult.FromCellIndex,
                ToCellIndex = moveResult.ToCellIndex,
                DiceValue = moveResult.DiceValue,
                HasExtraTurn = false,
                HasWon = moveResult.IsGameOver,
                Message = moveResult.ExtraInfo,
                EffectType = effectType,
                MessageIndex = moveResult.MessageIndex,
                GrantedItemCode = moveResult.GrantedItemCode,
                GrantedDiceCode = moveResult.GrantedDiceCode
            };
        }

        private static MoveEffectType MapMoveEffectType(string extraInfo)
        {
            if (string.IsNullOrWhiteSpace(extraInfo))
            {
                return MoveEffectType.None;
            }

            string normalized = extraInfo.ToUpperInvariant();

            if (normalized.Contains(EFFECT_TOKEN_LADDER))
            {
                return MoveEffectType.Ladder;
            }

            if (normalized.Contains(EFFECT_TOKEN_SNAKE))
            {
                return MoveEffectType.Snake;
            }

            return MoveEffectType.None;
        }
    }
}
