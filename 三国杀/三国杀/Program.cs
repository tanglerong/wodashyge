using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace ConsoleThreeKingdomsKill
{
    // AI难度枚举
    enum AIDifficulty { 简单, 普通, 困难 }
    // 卡牌类型
    enum CardType
    {
        杀, 闪, 桃,
        过河拆桥, 顺手牵羊, 桃园结义, 南蛮入侵, 万箭齐发, 无中生有,
        诸葛连弩, 青龙偃月刀, 丈八蛇矛, 方天画戟, 八卦阵, 仁王盾
    }
    // 装备类型
    enum EquipmentType { 武器, 防具 }
    // 武将枚举
    enum Warrior { 刘备, 曹操, 孙权, 诸葛亮, 关羽, 张飞, 赵云 }

    // 装备类
    class Equipment
    {
        public required CardType Card { get; set; }
        public required EquipmentType Type { get; set; }
        public required string Effect { get; set; }
        public int AttackRange { get; set; } = 1;
        public int HandCardLimitBonus { get; set; } = 0;
    }

    // 玩家类（Player）
    class Player
    {
        public required string Name { get; set; }
        public required Warrior WarriorType { get; set; }
        public int HP { get; set; } = 4;
        public List<CardType> HandCards { get; set; } = new List<CardType>();
        public Equipment CurrentWeapon { get; set; } = null;
        public Equipment CurrentArmor { get; set; } = null;
        public bool IsAlive => HP > 0;
        public bool IsHumanPlayer => Name == "你";
        public AIDifficulty AIDiff { get; set; }

        // 手牌上限
        public int CurrentHandCardLimit
        {
            get
            {
                int baseLimit = HP;
                int equipBonus = (CurrentWeapon?.HandCardLimitBonus ?? 0) + (CurrentArmor?.HandCardLimitBonus ?? 0);
                return Math.Max(baseLimit + equipBonus, 0);
            }
        }

        // 触发防具效果
        public bool TriggerArmorEffect(CardType attackCard)
        {
            if (CurrentArmor == null) return false;

            switch (CurrentArmor.Card)
            {
                case CardType.八卦阵:
                    Random rnd = new Random();
                    bool dodge = rnd.Next(2) == 0;
                    if (dodge)
                    {
                        Console.WriteLine($"{Name} 装备【八卦阵】生效，触发闪避！");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"{Name} 装备【八卦阵】生效，未触发闪避！");
                        return false;
                    }
                case CardType.仁王盾:
                    Random rnd2 = new Random();
                    bool block = rnd2.Next(2) == 0;
                    if (block)
                    {
                        Console.WriteLine($"{Name} 装备【仁王盾】生效，抵挡了【{attackCard}】！");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"{Name} 装备【仁王盾】生效，未抵挡【{attackCard}】！");
                        return false;
                    }
                default:
                    return false;
            }
        }

        // 仁德（刘备技能）
        public void RenDe(List<Player> allPlayers, Player currentPlayer)
        {
            Console.Write("\n是否发动【仁德】？（交出手牌给其他玩家，每交2张摸1张，输入y/n）：");
            Thread.Sleep(800);
            if (Console.ReadLine()?.ToLower() != "y" || HandCards.Count == 0)
            {
                Console.WriteLine("跳过【仁德】技能。");
                Thread.Sleep(800);
                return;
            }

            var targets = allPlayers.Where(p => p != currentPlayer && p.IsAlive).ToList();
            Console.Write($"选择要仁德的目标（{string.Join("、", targets.Select(t => t.Name))}）：");
            Thread.Sleep(800);
            var targetName = Console.ReadLine();
            var target = targets.FirstOrDefault(t => t.Name == targetName);
            if (target == null)
            {
                Console.WriteLine("目标无效，跳过【仁德】。");
                Thread.Sleep(800);
                return;
            }

            Console.WriteLine($"你的手牌：{string.Join("、", HandCards.Select((c, i) => $"{i + 1}.{c}"))}");
            Thread.Sleep(800);
            Console.Write("输入要交出的手牌序号（用逗号分隔，如1,2）：");
            var cardIndices = Console.ReadLine()?.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int idx) ? idx - 1 : -1)
                .Where(idx => idx >= 0 && idx < HandCards.Count)
                .Distinct()
                .ToList();

            if (cardIndices == null || cardIndices.Count == 0)
            {
                Console.WriteLine("未选择有效手牌，跳过【仁德】。");
                Thread.Sleep(800);
                return;
            }

            var givenCards = new List<CardType>();
            foreach (var idx in cardIndices.OrderByDescending(i => i))
            {
                givenCards.Add(HandCards[idx]);
                HandCards.RemoveAt(idx);
            }
            target.HandCards.AddRange(givenCards);
            Console.WriteLine($"{Name} 向 {target.Name} 仁德了【{string.Join("、", givenCards)}】！");
            Thread.Sleep(800);

            int drawCount = givenCards.Count / 2;
            if (drawCount > 0)
            {
                Program.DrawCards(this, drawCount);
                Console.WriteLine($"{Name} 仁德触发摸牌，摸了{drawCount}张牌！");
                Thread.Sleep(800);
                Console.WriteLine($"当前手牌：{string.Join("、", HandCards)}");
                Thread.Sleep(800);
            }
        }

        // 奸雄（曹操技能）
        public void JianXiong(CardType damageCard)
        {
            if (!IsAlive) return;
            Console.Write($"\n是否发动【奸雄】？（获得造成伤害的【{damageCard}】，输入y/n）：");
            Thread.Sleep(800);
            if (Console.ReadLine()?.ToLower() == "y")
            {
                HandCards.Add(damageCard);
                Console.WriteLine($"{Name} 发动【奸雄】，获得了【{damageCard}】！");
                Thread.Sleep(800);
                Console.WriteLine($"当前手牌：{string.Join("、", HandCards)}");
                Thread.Sleep(800);
            }
        }

        // 制衡（孙权技能）
        public void ZhiHeng()
        {
            Console.Write("\n是否发动【制衡】？（弃置手牌后摸等量牌，输入y/n）：");
            Thread.Sleep(800);
            if (Console.ReadLine()?.ToLower() != "y")
            {
                Console.WriteLine("跳过【制衡】技能。");
                Thread.Sleep(800);
                return;
            }

            if (HandCards.Count == 0)
            {
                Console.WriteLine("没有手牌可制衡，跳过【制衡】。");
                Thread.Sleep(800);
                return;
            }

            Console.WriteLine($"你的手牌：{string.Join("、", HandCards.Select((c, i) => $"{i + 1}.{c}"))}");
            Thread.Sleep(800);
            Console.Write("输入要制衡的手牌序号（用逗号分隔，如1,2）：");
            var cardIndices = Console.ReadLine()?.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int idx) ? idx - 1 : -1)
                .Where(idx => idx >= 0 && idx < HandCards.Count)
                .Distinct()
                .ToList();

            if (cardIndices == null || cardIndices.Count == 0)
            {
                Console.WriteLine("未选择有效手牌，跳过【制衡】。");
                Thread.Sleep(800);
                return;
            }

            int discardCount = cardIndices.Count;
            var discardedCards = new List<CardType>();
            foreach (var idx in cardIndices.OrderByDescending(i => i))
            {
                discardedCards.Add(HandCards[idx]);
                HandCards.RemoveAt(idx);
            }
            Console.WriteLine($"{Name} 发动【制衡】，弃置了【{string.Join("、", discardedCards)}】！");
            Thread.Sleep(800);

            Program.DrawCards(this, discardCount);
            Console.WriteLine($"制衡摸牌{discardCount}张，当前手牌：{string.Join("、", HandCards)}");
            Thread.Sleep(800);
        }

        // 观星（诸葛亮技能）
        public void GuanXing(Queue<CardType> cardDeck)
        {
            Console.Write("\n是否发动【观星】？（观看牌堆顶3张牌并调整，输入y/n）：");
            Thread.Sleep(800);
            if (Console.ReadLine()?.ToLower() != "y")
            {
                Console.WriteLine("跳过【观星】技能。");
                Thread.Sleep(800);
                return;
            }

            var starCards = new List<CardType>();
            int takeCount = Math.Min(3, cardDeck.Count);
            for (int i = 0; i < takeCount; i++)
            {
                starCards.Add(cardDeck.Dequeue());
            }

            Console.WriteLine($"牌堆顶3张牌：{string.Join("、", starCards.Select((c, i) => $"{i + 1}.{c}"))}");
            Thread.Sleep(800);
            Console.Write("请输入调整后的牌序（输入序号，用逗号分隔，如3,1,2；多余牌自动弃置）：");
            var newOrder = Console.ReadLine()?.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int idx) ? idx - 1 : -1)
                .Where(idx => idx >= 0 && idx < starCards.Count)
                .Distinct()
                .ToList();

            var orderedCards = new List<CardType>();
            foreach (var idx in newOrder)
            {
                orderedCards.Add(starCards[idx]);
            }

            foreach (var card in orderedCards)
            {
                cardDeck.Enqueue(card);
            }

            var discardedCards = starCards.Where(c => !orderedCards.Contains(c)).ToList();
            if (discardedCards.Count > 0)
            {
                Console.WriteLine($"弃置观星多余牌：【{string.Join("、", discardedCards)}】");
                Thread.Sleep(800);
            }

            Console.WriteLine($"观星结束，牌堆顶已调整为：【{string.Join("、", orderedCards)}】");
            Thread.Sleep(800);
        }

        // 武圣（关羽技能）
        public void WuSheng(out CardType usedCard)
        {
            usedCard = CardType.杀;
            Console.Write("\n是否发动【武圣】？（将任意手牌当作【杀】使用，输入y/n）：");
            Thread.Sleep(800);
            if (Console.ReadLine()?.ToLower() != "y" || HandCards.Count == 0)
            {
                Console.WriteLine("跳过【武圣】技能。");
                Thread.Sleep(800);
                return;
            }

            Console.WriteLine($"你的手牌：{string.Join("、", HandCards.Select((c, i) => $"{i + 1}.{c}"))}");
            Thread.Sleep(800);
            Console.Write("选择要当作【杀】使用的手牌序号：");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int idx) && idx >= 1 && idx <= HandCards.Count)
            {
                usedCard = HandCards[idx - 1];
                HandCards.RemoveAt(idx - 1);
                Console.WriteLine($"{Name} 发动【武圣】，将【{usedCard}】当作【杀】使用！");
                Thread.Sleep(800);
            }
            else
            {
                Console.WriteLine("选择无效，跳过【武圣】。");
                Thread.Sleep(800);
            }
        }

        // 咆哮（张飞技能）
        public bool PaoXiao()
        {
            Console.Write("\n是否发动【咆哮】？（本回合可出多张【杀】，输入y/n）：");
            Thread.Sleep(800);
            return Console.ReadLine()?.ToLower() == "y";
        }

        // 龙胆（赵云技能）
        public CardType LongDan(CardType targetType)
        {
            Console.Write($"\n是否发动【龙胆】？（将{(targetType == CardType.杀 ? "闪" : "杀")}当作【{targetType}】使用，输入y/n）：");
            Thread.Sleep(800);
            if (Console.ReadLine()?.ToLower() != "y")
            {
                return targetType;
            }

            var sourceType = targetType == CardType.杀 ? CardType.闪 : CardType.杀;
            if (HandCards.Contains(sourceType))
            {
                HandCards.Remove(sourceType);
                Console.WriteLine($"{Name} 发动【龙胆】，将【{sourceType}】当作【{targetType}】使用！");
                Thread.Sleep(800);
                return targetType;
            }
            else
            {
                Console.WriteLine($"没有【{sourceType}】，无法发动【龙胆】。");
                Thread.Sleep(800);
                return targetType;
            }
        }

        // 使用桃
        public void UsePeach()
        {
            if (HP >= 4)
            {
                Console.WriteLine("体力已满，无法使用【桃】！");
                Thread.Sleep(800);
                return;
            }
            HandCards.Remove(CardType.桃);
            HP++;
            Console.WriteLine($"{Name} 使用【桃】回复1点体力，当前体力：{HP}");
            Thread.Sleep(800);
        }

        // 装备方法
        public void Equip(Equipment newEquip)
        {
            if (newEquip.Type == EquipmentType.武器)
            {
                if (CurrentWeapon != null)
                {
                    Console.WriteLine($"【AI】{Name} 卸下【{CurrentWeapon.Card}】，装备【{newEquip.Card}】");
                }
                else
                {
                    Console.WriteLine($"【AI】{Name} 装备【{newEquip.Card}】");
                }
                CurrentWeapon = newEquip;
            }
            else if (newEquip.Type == EquipmentType.防具)
            {
                if (CurrentArmor != null)
                {
                    Console.WriteLine($"【AI】{Name} 卸下【{CurrentArmor.Card}】，装备【{newEquip.Card}】");
                }
                else
                {
                    Console.WriteLine($"【AI】{Name} 装备【{newEquip.Card}】");
                }
                CurrentArmor = newEquip;
            }
        }

        // AI出牌阶段
        public void AIPlayCardPhase(List<Player> allPlayers)
        {
            Console.WriteLine($"\n===== 【AI-{AIDiff}】{Name} 的出牌阶段 =====");
            Thread.Sleep(1200);

            bool hasZhuGeLianNu = CurrentWeapon?.Card == CardType.诸葛连弩;
            bool canMultiKill = (hasZhuGeLianNu || WarriorType == Warrior.张飞) && AIDiff != AIDifficulty.简单;
            int killCount = 0;

            // 装备选择
            if (AIDiff != AIDifficulty.简单)
            {
                var weaponCards = HandCards.Where(c => Program.EquipmentConfig.ContainsKey(c) && Program.EquipmentConfig[c].Type == EquipmentType.武器).ToList();
                if (weaponCards.Count > 0)
                {
                    var bestWeapon = weaponCards.OrderByDescending(c => Program.EquipmentConfig[c].AttackRange).First();
                    HandCards.Remove(bestWeapon);
                    Console.WriteLine($"【AI】{Name} 装备了【{bestWeapon}】（攻击距离{Program.EquipmentConfig[bestWeapon].AttackRange}）");
                    Thread.Sleep(1200);
                    Equip(Program.EquipmentConfig[bestWeapon]);
                    Thread.Sleep(1200);
                }

                if (AIDiff == AIDifficulty.困难)
                {
                    var armorCards = HandCards.Where(c => Program.EquipmentConfig.ContainsKey(c) && Program.EquipmentConfig[c].Type == EquipmentType.防具).ToList();
                    if (armorCards.Count > 0 && CurrentArmor == null)
                    {
                        var bestArmor = armorCards.First();
                        HandCards.Remove(bestArmor);
                        Console.WriteLine($"【AI】{Name} 装备了【{bestArmor}】");
                        Thread.Sleep(1200);
                        Equip(Program.EquipmentConfig[bestArmor]);
                        Thread.Sleep(1200);
                    }
                }
            }

            // AOE锦囊使用
            if (AIDiff != AIDifficulty.简单)
            {
                var aoeTricks = HandCards.Where(c => c == CardType.南蛮入侵 || c == CardType.万箭齐发).ToList();
                foreach (var aoe in aoeTricks)
                {
                    HandCards.Remove(aoe);
                    Console.WriteLine($"【AI】{Name} 使用了【{aoe}】！");
                    Thread.Sleep(1200);
                    AIHandleAOETrick(aoe, allPlayers);
                    Thread.Sleep(1200);
                    if (HandCards.Count == 0) break;
                }
            }

            // 控制锦囊使用
            if (AIDiff == AIDifficulty.困难)
            {
                var targetPlayer = allPlayers.FirstOrDefault(p => p.IsHumanPlayer && p.IsAlive)
                                 ?? allPlayers.Where(p => p.IsAlive && p != this).OrderBy(p => p.HP).First();
                var controlTricks = HandCards.Where(c => c == CardType.过河拆桥 || c == CardType.顺手牵羊).ToList();
                foreach (var trick in controlTricks)
                {
                    HandCards.Remove(trick);
                    Console.WriteLine($"【AI】{Name} 对 {targetPlayer.Name} 使用了【{trick}】！");
                    Thread.Sleep(1200);
                    AIHandleControlTrick(trick, targetPlayer);
                    Thread.Sleep(1200);
                    if (HandCards.Count == 0) break;
                }
            }

            // 出杀逻辑
            Player attackTarget = null;
            var validTargets = GetValidTargetsByRange(allPlayers, CurrentWeapon?.AttackRange ?? 1);
            if (validTargets.Count > 0)
            {
                if (AIDiff == AIDifficulty.困难)
                {
                    attackTarget = validTargets.FirstOrDefault(p => p.IsHumanPlayer) ?? validTargets.OrderBy(p => p.HP).First();
                }
                else if (AIDiff == AIDifficulty.普通)
                {
                    attackTarget = validTargets.OrderBy(_ => Guid.NewGuid()).First();
                }
                else
                {
                    attackTarget = validTargets.First();
                }
            }

            int maxKillCount = AIDiff == AIDifficulty.困难 ? 3 : (AIDiff == AIDifficulty.普通 ? 2 : 1);
            while (killCount < maxKillCount && validTargets.Count > 0 && attackTarget != null)
            {
                if (!HandCards.Contains(CardType.杀) && !(WarriorType == Warrior.关羽 && AIDiff != AIDifficulty.简单 && HandCards.Count > 0)) break;

                Console.WriteLine($"【AI】{Name} 对 {attackTarget.Name} 出【杀】！");
                Thread.Sleep(1200);
                AIHandleSha(attackTarget, ref killCount, canMultiKill);
                Thread.Sleep(1200);

                killCount++;
            }

            Console.WriteLine($"【AI】{Name} 结束出牌阶段");
            Thread.Sleep(1200);
        }

        // AI处理AOE锦囊
        private void AIHandleAOETrick(CardType aoeCard, List<Player> allPlayers)
        {
            if (aoeCard == CardType.南蛮入侵)
            {
                Console.WriteLine($"【AI】{Name} 发动【南蛮入侵】！所有玩家需出杀！");
                foreach (var p in allPlayers.Where(p => p.IsAlive && p != this))
                {
                    Thread.Sleep(800);
                    bool hasSha = p.HandCards.Contains(CardType.杀);
                    if (AIDiff == AIDifficulty.简单 && new Random().Next(2) == 0) hasSha = false;

                    if (hasSha)
                    {
                        p.HandCards.Remove(CardType.杀);
                        Console.WriteLine($"{p.Name} 出【杀】抵消南蛮入侵");
                    }
                    else
                    {
                        p.HP--;
                        Console.WriteLine($"{p.Name} 无法出杀，体力变为{p.HP}");
                        if (!p.IsAlive) Console.WriteLine($"{p.Name} 阵亡！");
                    }
                }
            }
            else if (aoeCard == CardType.万箭齐发)
            {
                Console.WriteLine($"【AI】{Name} 发动【万箭齐发】！所有玩家需出闪！");
                foreach (var p in allPlayers.Where(p => p.IsAlive && p != this))
                {
                    Thread.Sleep(800);
                    bool hasShan = p.HandCards.Contains(CardType.闪);
                    if (AIDiff == AIDifficulty.简单 && new Random().Next(2) == 0) hasShan = false;

                    if (hasShan)
                    {
                        p.HandCards.Remove(CardType.闪);
                        Console.WriteLine($"{p.Name} 出【闪】抵消万箭齐发");
                    }
                    else
                    {
                        p.HP--;
                        Console.WriteLine($"{p.Name} 无法出闪，体力变为{p.HP}");
                        if (!p.IsAlive) Console.WriteLine($"{p.Name} 阵亡！");
                    }
                }
            }
        }

        // AI处理控制锦囊
        private void AIHandleControlTrick(CardType trickCard, Player target)
        {
            if (trickCard == CardType.过河拆桥)
            {
                CardType targetCard;
                if (AIDiff == AIDifficulty.困难)
                {
                    var tempCard = target.HandCards.FirstOrDefault(c => c == CardType.桃 || c == CardType.杀 || Program.EquipmentConfig.ContainsKey(c));
                    targetCard = tempCard != default(CardType) ? tempCard : target.HandCards.First();
                }
                else
                {
                    targetCard = target.HandCards.OrderBy(_ => Guid.NewGuid()).First();
                }
                target.HandCards.Remove(targetCard);
                Console.WriteLine($"【AI】{Name} 拆走了 {target.Name} 的【{targetCard}】");
            }
            else if (trickCard == CardType.顺手牵羊)
            {
                CardType targetCard;
                if (AIDiff == AIDifficulty.困难)
                {
                    var tempCard = target.HandCards.FirstOrDefault(c => c == CardType.桃 || c == CardType.杀 || Program.EquipmentConfig.ContainsKey(c));
                    targetCard = tempCard != default(CardType) ? tempCard : target.HandCards.First();
                }
                else
                {
                    targetCard = target.HandCards.OrderBy(_ => Guid.NewGuid()).First();
                }
                target.HandCards.Remove(targetCard);
                HandCards.Add(targetCard);
                Console.WriteLine($"【AI】{Name} 牵走了 {target.Name} 的【{targetCard}】");
            }
        }

        // AI处理出杀
        private void AIHandleSha(Player target, ref int killCount, bool canMultiKill)
        {
            bool dodge = target.HandCards.Contains(CardType.闪) && target.HP > 1;
            if (AIDiff == AIDifficulty.简单 && new Random().Next(2) == 0) dodge = false;

            if (dodge)
            {
                target.HandCards.Remove(CardType.闪);
                Console.WriteLine($"{target.Name} 出【闪】抵消了【杀】");
                if (AIDiff == AIDifficulty.困难 && CurrentWeapon?.Card == CardType.青龙偃月刀 && HandCards.Contains(CardType.杀))
                {
                    Thread.Sleep(800);
                    Console.WriteLine($"【AI】{Name} 发动青龙偃月刀，再出1张【杀】！");
                    HandCards.Remove(CardType.杀);
                    target.HP--;
                    Console.WriteLine($"{target.Name} 体力变为{target.HP}");
                }
                return;
            }

            target.HP--;
            Console.WriteLine($"{target.Name} 未出闪，体力变为{target.HP}");
            if (!target.IsAlive) Console.WriteLine($"{target.Name} 阵亡！");
        }

        // AI弃牌阶段
        public void AIDiscardPhase()
        {
            Console.WriteLine($"\n===== 【AI-{AIDiff}】{Name} 的弃牌阶段 =====");
            Thread.Sleep(1200);

            Console.WriteLine($"【AI】{Name} 手牌数：{HandCards.Count}，手牌上限：{CurrentHandCardLimit}");
            Thread.Sleep(800);

            int excessCount = HandCards.Count - CurrentHandCardLimit;
            if (excessCount <= 0)
            {
                Console.WriteLine($"【AI】{Name} 手牌数未超过上限，无需弃牌");
                Thread.Sleep(1200);
                return;
            }

            Console.WriteLine($"【AI】{Name} 需弃置 {excessCount} 张牌");
            Thread.Sleep(800);

            IOrderedEnumerable<CardType> discardPriority;
            if (AIDiff == AIDifficulty.困难)
            {
                discardPriority = HandCards.OrderBy(c =>
                    c == CardType.闪 ? 0 :
                    c == CardType.杀 && HandCards.Count(c2 => c2 == CardType.杀) > 2 ? 1 :
                    c == CardType.过河拆桥 || c == CardType.顺手牵羊 ? 2 : 3);
            }
            else if (AIDiff == AIDifficulty.普通)
            {
                discardPriority = HandCards.OrderBy(_ => Guid.NewGuid());
            }
            else
            {
                discardPriority = HandCards.OrderBy(_ => Guid.NewGuid());
            }

            for (int i = 0; i < excessCount; i++)
            {
                var discardCard = discardPriority.ElementAt(i);
                HandCards.Remove(discardCard);
                Console.WriteLine($"【AI】{Name} 弃置了1张牌");
                Thread.Sleep(800);
            }

            Console.WriteLine($"【AI】{Name} 弃牌结束");
            Thread.Sleep(1200);
        }

        // 获取攻击范围内目标
        private List<Player> GetValidTargetsByRange(List<Player> allPlayers, int attackRange)
        {
            var currentIndex = allPlayers.IndexOf(this);
            return allPlayers.Where(p =>
                p.IsAlive && p != this &&
                Math.Min(Math.Abs(allPlayers.IndexOf(p) - currentIndex), allPlayers.Count - Math.Abs(allPlayers.IndexOf(p) - currentIndex)) <= attackRange
            ).ToList();
        }

        // 人类玩家弃牌阶段
        public void DiscardPhase()
        {
            Console.WriteLine("\n===== 你的弃牌阶段 =====");
            Thread.Sleep(800);
            Console.WriteLine($"当前体力：{HP}，手牌上限：{CurrentHandCardLimit}");
            Thread.Sleep(800);
            Console.WriteLine($"当前手牌：{string.Join("、", HandCards.Select((c, i) => $"{i + 1}.{c}"))}");
            Thread.Sleep(800);

            int excessCount = HandCards.Count - CurrentHandCardLimit;
            if (excessCount <= 0)
            {
                Console.WriteLine("手牌数未超过上限，无需弃牌！");
                Thread.Sleep(800);
                return;
            }

            Console.WriteLine($"需弃置 {excessCount} 张牌（手牌数不得超过当前体力值）！");
            Thread.Sleep(800);
            while (HandCards.Count > CurrentHandCardLimit)
            {
                Console.Write($"请选择要弃置的手牌序号（剩余需弃 {HandCards.Count - CurrentHandCardLimit} 张）：");
                if (int.TryParse(Console.ReadLine()?.Trim(), out int idx) && idx >= 1 && idx <= HandCards.Count)
                {
                    var discardedCard = HandCards[idx - 1];
                    HandCards.RemoveAt(idx - 1);
                    Console.WriteLine($"弃置了【{discardedCard}】，剩余手牌：{string.Join("、", HandCards)}");
                    Thread.Sleep(800);
                }
                else
                {
                    Console.WriteLine("选择无效，请重新输入！");
                    Thread.Sleep(800);
                }
            }

            Console.WriteLine("弃牌阶段结束！");
            Thread.Sleep(800);
        }
    }

    class Program
    {
        static List<Player> players = new List<Player>();
        static Queue<CardType> cardDeck = new Queue<CardType>();
        static int currentPlayerIndex = 0;
        public static Dictionary<CardType, Equipment> EquipmentConfig = new Dictionary<CardType, Equipment>()
        {
            { CardType.诸葛连弩, new Equipment {
                Card = CardType.诸葛连弩,
                Type = EquipmentType.武器,
                Effect = "出牌阶段可出任意数量的【杀】",
                AttackRange = 1,
                HandCardLimitBonus = 1
            }},
            { CardType.青龙偃月刀, new Equipment {
                Card = CardType.青龙偃月刀,
                Type = EquipmentType.武器,
                Effect = "当【杀】被闪避后，可再出1张【杀】指定同一目标",
                AttackRange = 3
            }},
            { CardType.丈八蛇矛, new Equipment {
                Card = CardType.丈八蛇矛,
                Type = EquipmentType.武器,
                Effect = "可将任意2张手牌当作【杀】使用",
                AttackRange = 2
            }},
            { CardType.方天画戟, new Equipment {
                Card = CardType.方天画戟,
                Type = EquipmentType.武器,
                Effect = "出牌阶段最后1张【杀】可指定任意数量目标",
                AttackRange = 4
            }},
            { CardType.八卦阵, new Equipment {
                Card = CardType.八卦阵,
                Type = EquipmentType.防具,
                Effect = "受到【杀】攻击时，50%概率闪避",
                HandCardLimitBonus = 1
            }},
            { CardType.仁王盾, new Equipment {
                Card = CardType.仁王盾,
                Type = EquipmentType.防具,
                Effect = "受到【杀】攻击时，50%概率抵挡",
                HandCardLimitBonus = 1
            }}
        };

        static void Main(string[] args)
        {
            Console.WriteLine("=== 三国杀（完整功能版）===");
            Console.WriteLine("1. 新游戏");
            Console.WriteLine("2. 读取存档");
            Console.Write("请选择操作（1/2）：");
            var choice = Console.ReadLine();

            if (choice == "2" && File.Exists("savegame.txt"))
            {
                LoadGame();
                Console.WriteLine("存档读取成功！");
                Thread.Sleep(1000);
            }
            else
            {
                // 新游戏流程
                AIDifficulty selectedDiff = SelectAIDifficulty();
                Warrior playerWarrior = SelectPlayerWarrior();
                InitPlayers(selectedDiff, playerWarrior);
                InitCardDeck();
                DealInitialCards();
            }

            GameLoop();
        }

        // 选择AI难度
        static AIDifficulty SelectAIDifficulty()
        {
            Console.WriteLine("\n=== 选择AI难度 ===");
            Console.WriteLine("1. 简单（AI策略差，容易失误）");
            Console.WriteLine("2. 普通（AI策略常规，平衡体验）");
            Console.WriteLine("3. 困难（AI策略激进，优先针对玩家）");
            Console.Write("请输入选择（1/2/3）：");

            while (true)
            {
                var input = Console.ReadLine();
                switch (input)
                {
                    case "1": return AIDifficulty.简单;
                    case "2": return AIDifficulty.普通;
                    case "3": return AIDifficulty.困难;
                    default:
                        Console.Write("输入无效，请重新输入（1/2/3）：");
                        break;
                }
            }
        }

        // 玩家选择武将
        static Warrior SelectPlayerWarrior()
        {
            Console.WriteLine("\n=== 选择你的武将 ===");
            var allWarriors = Enum.GetValues(typeof(Warrior))
                                  .Cast<Warrior>()
                                  .ToList();

            for (int i = 0; i < allWarriors.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {allWarriors[i]}");
            }

            Console.Write("请输入选择（序号）：");
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= allWarriors.Count)
                {
                    Warrior selected = allWarriors[choice - 1];
                    Console.WriteLine($"你选择了武将：{selected}\n");
                    Thread.Sleep(1000);
                    return selected;
                }
                Console.Write("输入无效，请重新输入序号：");
            }
        }

        // 初始化玩家
        static void InitPlayers(AIDifficulty aiDiff, Warrior playerWarrior)
        {
            // 人类玩家
            players.Add(new Player
            {
                Name = "你",
                WarriorType = playerWarrior,
                HP = 4
            });

            // AI玩家（随机选剩余3个武将）
            var remainingWarriors = Enum.GetValues(typeof(Warrior))
                                        .Cast<Warrior>()
                                        .Where(w => w != playerWarrior)
                                        .ToList();

            var aiWarriors = remainingWarriors.OrderBy(_ => Guid.NewGuid()).Take(3).ToList();

            foreach (var warrior in aiWarriors)
            {
                players.Add(new Player
                {
                    Name = warrior.ToString(),
                    WarriorType = warrior,
                    HP = 4,
                    AIDiff = aiDiff
                });
            }

            // 打印玩家列表
            Console.WriteLine("=== 本局玩家 ===");
            foreach (var p in players)
            {
                Console.WriteLine($"{p.Name}（武将：{p.WarriorType}，体力：{p.HP}）");
                Thread.Sleep(500);
            }
            Console.WriteLine();
            Thread.Sleep(1000);
        }

        // 初始化牌堆
        static void InitCardDeck()
        {
            // 基础牌
            for (int i = 0; i < 12; i++) cardDeck.Enqueue(CardType.杀);
            for (int i = 0; i < 8; i++) cardDeck.Enqueue(CardType.闪);
            for (int i = 0; i < 6; i++) cardDeck.Enqueue(CardType.桃);
            // 锦囊牌
            for (int i = 0; i < 3; i++) cardDeck.Enqueue(CardType.过河拆桥);
            for (int i = 0; i < 3; i++) cardDeck.Enqueue(CardType.顺手牵羊);
            cardDeck.Enqueue(CardType.桃园结义);
            cardDeck.Enqueue(CardType.南蛮入侵);
            cardDeck.Enqueue(CardType.万箭齐发);
            for (int i = 0; i < 2; i++) cardDeck.Enqueue(CardType.无中生有);
            // 装备牌
            foreach (var equipCard in EquipmentConfig.Keys)
            {
                cardDeck.Enqueue(equipCard);
            }

            // 洗牌
            cardDeck = new Queue<CardType>(cardDeck.OrderBy(_ => Guid.NewGuid()));
        }

        // 发初始手牌
        static void DealInitialCards()
        {
            foreach (var p in players)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (cardDeck.Count == 0) InitCardDeck();
                    p.HandCards.Add(cardDeck.Dequeue());
                }
                if (p.IsHumanPlayer)
                {
                    Console.WriteLine($"{p.Name} 获得初始手牌：{string.Join("、", p.HandCards)}");
                }
                else
                {
                    Console.WriteLine($"{p.Name} 获得初始手牌：******（共4张）");
                }
                Thread.Sleep(1000);
            }
        }

        // 游戏主循环
        static void GameLoop()
        {
            while (players.Count(p => p.IsAlive) > 1)
            {
                var currentPlayer = players[currentPlayerIndex];
                if (!currentPlayer.IsAlive)
                {
                    NextPlayer();
                    continue;
                }

                Console.WriteLine($"\n===== {currentPlayer.Name} 的回合 =====");
                Thread.Sleep(1500);

                // 显示状态
                string weaponName = currentPlayer.CurrentWeapon != null ? currentPlayer.CurrentWeapon.Card.ToString() : "无";
                string armorName = currentPlayer.CurrentArmor != null ? currentPlayer.CurrentArmor.Card.ToString() : "无";
                Console.WriteLine($"当前体力：{currentPlayer.HP}，手牌上限：{currentPlayer.CurrentHandCardLimit}");
                Console.WriteLine($"当前装备：武器【{weaponName}】 | 防具【{armorName}】");
                if (currentPlayer.IsHumanPlayer)
                {
                    Console.WriteLine($"当前手牌：{string.Join("、", currentPlayer.HandCards.Select((c, i) => $"{i + 1}.{c}"))}");
                }
                else
                {
                    Console.WriteLine($"当前手牌：******（共{currentPlayer.HandCards.Count}张）");
                }
                Thread.Sleep(1200);

                // 存档提示（人类玩家回合开始时）
                if (currentPlayer.IsHumanPlayer)
                {
                    Console.Write("\n是否存档？（y/n）：");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        SaveGame();
                        Console.WriteLine("存档成功！");
                        Thread.Sleep(1000);
                    }
                }

                // 摸牌阶段
                DrawCards(currentPlayer, 2);
                if (currentPlayer.IsHumanPlayer)
                {
                    Console.WriteLine($"摸牌后手牌：{string.Join("、", currentPlayer.HandCards)}");
                }
                else
                {
                    Console.WriteLine($"摸牌后手牌：******（共{currentPlayer.HandCards.Count}张）");
                }
                Thread.Sleep(1200);

                // 武将技能阶段
                if (!currentPlayer.IsHumanPlayer)
                {
                    if (currentPlayer.WarriorType == Warrior.诸葛亮 && currentPlayer.AIDiff != AIDifficulty.简单)
                    {
                        Console.WriteLine($"【AI】{currentPlayer.Name} 发动【观星】！");
                        Thread.Sleep(1200);
                        var starCards = new List<CardType>();
                        int takeCount = Math.Min(3, cardDeck.Count);
                        for (int i = 0; i < takeCount; i++) starCards.Add(cardDeck.Dequeue());
                        var keepCards = starCards.Where(c => c == CardType.桃 || c == CardType.杀).OrderBy(c => c).ToList();
                        var discardCards = starCards.Except(keepCards).ToList();
                        foreach (var card in keepCards) cardDeck.Enqueue(card);
                        Console.WriteLine($"【AI】{currentPlayer.Name} 观星调整牌堆");
                        Thread.Sleep(1200);
                    }
                }
                else
                {
                    TriggerWarriorSkill(currentPlayer);
                }

                // 出牌阶段
                if (currentPlayer.IsHumanPlayer)
                {
                    PlayCardPhase(currentPlayer);
                }
                else
                {
                    currentPlayer.AIPlayCardPhase(players);
                }

                // 弃牌阶段
                if (currentPlayer.IsHumanPlayer)
                {
                    currentPlayer.DiscardPhase();
                }
                else
                {
                    currentPlayer.AIDiscardPhase();
                }

                // 结束回合
                NextPlayer();
            }

            var winner = players.First(p => p.IsAlive);
            Console.WriteLine($"\n===== 游戏结束 =====");
            Console.WriteLine($"{winner.Name} 获得胜利！");
            Console.ReadKey();
        }

        // 人类玩家出牌阶段
        static void PlayCardPhase(Player currentPlayer)
        {
            Console.WriteLine("\n===== 你的出牌阶段 =====");
            Thread.Sleep(800);
            bool hasZhuGeLianNu = currentPlayer.CurrentWeapon?.Card == CardType.诸葛连弩;
            bool canMultiKill = hasZhuGeLianNu || (currentPlayer.WarriorType == Warrior.张飞 && currentPlayer.PaoXiao());
            int killCount = 0;

            while (true)
            {
                Console.WriteLine($"\n当前手牌：{string.Join("、", currentPlayer.HandCards.Select((c, i) => $"{i + 1}.{c}"))}");
                Thread.Sleep(800);
                Console.Write("选择要出的牌序号（0=结束出牌）：");
                if (!int.TryParse(Console.ReadLine()?.Trim(), out int choice) || choice < 0 || choice > currentPlayer.HandCards.Count)
                {
                    Console.WriteLine("选择无效，请重新输入！");
                    Thread.Sleep(800);
                    continue;
                }

                if (choice == 0)
                {
                    Console.WriteLine("结束出牌阶段。");
                    Thread.Sleep(800);
                    break;
                }

                CardType playedCard = currentPlayer.HandCards[choice - 1];
                currentPlayer.HandCards.RemoveAt(choice - 1);
                Console.WriteLine($"{currentPlayer.Name} 使用了【{playedCard}】！");
                Thread.Sleep(800);

                if (EquipmentConfig.ContainsKey(playedCard))
                {
                    currentPlayer.Equip(EquipmentConfig[playedCard]);
                    Thread.Sleep(800);
                }
                else
                {
                    switch (playedCard)
                    {
                        case CardType.杀:
                            killCount++;
                            bool isLastKill = currentPlayer.HandCards.Count == 0;
                            HandleSha(currentPlayer, ref killCount, canMultiKill, isLastKill);
                            break;

                        case CardType.桃:
                            currentPlayer.UsePeach();
                            break;

                        case CardType.过河拆桥:
                            HandleHeGuoChaiQiao(currentPlayer);
                            break;

                        case CardType.顺手牵羊:
                            HandleShunShouQianYang(currentPlayer);
                            break;

                        case CardType.桃园结义:
                            HandleTaoYuanJieYi(currentPlayer);
                            break;

                        case CardType.南蛮入侵:
                            HandleNanManRuQin(currentPlayer);
                            break;

                        case CardType.万箭齐发:
                            HandleWanJianQiFa(currentPlayer);
                            break;

                        case CardType.无中生有:
                            HandleWuZhongShengYou(currentPlayer);
                            break;
                    }
                }

                string weaponName = currentPlayer.CurrentWeapon != null ? currentPlayer.CurrentWeapon.Card.ToString() : "无";
                string armorName = currentPlayer.CurrentArmor != null ? currentPlayer.CurrentArmor.Card.ToString() : "无";
                Console.WriteLine($"当前装备：武器【{weaponName}】 | 防具【{armorName}】");
                Thread.Sleep(800);
                Console.WriteLine($"当前手牌：{string.Join("、", currentPlayer.HandCards)}");
                Thread.Sleep(800);
            }
        }

        // 触发人类玩家武将技能
        static void TriggerWarriorSkill(Player currentPlayer)
        {
            switch (currentPlayer.WarriorType)
            {
                case Warrior.刘备:
                    currentPlayer.RenDe(players, currentPlayer);
                    break;
                case Warrior.孙权:
                    currentPlayer.ZhiHeng();
                    break;
                case Warrior.诸葛亮:
                    currentPlayer.GuanXing(cardDeck);
                    break;
                default:
                    Console.WriteLine("无主动技能可触发，跳过技能阶段。");
                    Thread.Sleep(800);
                    break;
            }
        }

        // 摸牌方法
        public static void DrawCards(Player player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (cardDeck.Count == 0) InitCardDeck();
                player.HandCards.Add(cardDeck.Dequeue());
            }
        }

        // 处理杀
        static void HandleSha(Player currentPlayer, ref int killCount, bool canMultiKill, bool isLastKill)
        {
            if (!canMultiKill && killCount > 1)
            {
                Console.WriteLine("无法出多张【杀】！");
                Thread.Sleep(800);
                return;
            }

            int attackRange = currentPlayer.CurrentWeapon?.AttackRange ?? 1;
            var validTargets = GetValidTargetsByRange(currentPlayer, attackRange);
            if (validTargets.Count == 0)
            {
                Console.WriteLine("无有效目标（攻击距离不足）！");
                Thread.Sleep(800);
                return;
            }

            List<Player> targets = new List<Player>();
            if (currentPlayer.CurrentWeapon?.Card == CardType.方天画戟 && isLastKill)
            {
                Console.Write($"方天画戟生效！选择目标（可多选，用逗号分隔：{string.Join("、", validTargets.Select(t => t.Name))}）：");
                Thread.Sleep(800);
                var targetNames = Console.ReadLine()?.Split(',')?.Select(s => s.Trim()).ToList() ?? new List<string>();
                targets = validTargets.Where(t => targetNames.Contains(t.Name)).ToList();
                if (targets.Count == 0)
                {
                    Console.WriteLine("目标无效，【杀】使用失败！");
                    Thread.Sleep(800);
                    return;
                }
            }
            else
            {
                Console.Write($"选择目标（攻击距离{attackRange}：{string.Join("、", validTargets.Select(t => t.Name))}）：");
                Thread.Sleep(800);
                var targetName = Console.ReadLine();
                var target = validTargets.FirstOrDefault(t => t.Name == targetName);
                if (target == null)
                {
                    Console.WriteLine("目标无效，【杀】使用失败！");
                    Thread.Sleep(800);
                    return;
                }
                targets.Add(target);
            }

            foreach (var target in targets)
            {
                Console.WriteLine($"\n{currentPlayer.Name} 对 {target.Name} 出【杀】！");
                Thread.Sleep(800);
                if (target.TriggerArmorEffect(CardType.杀))
                {
                    Console.WriteLine($"{target.Name} 防御成功，未受到伤害！");
                    Thread.Sleep(800);
                    continue;
                }

                bool isDodged = false;
                if (target.HandCards.Contains(CardType.闪) || (target.WarriorType == Warrior.赵云 && target.HandCards.Contains(CardType.杀)))
                {
                    Console.Write($"{target.Name} 是否出【闪】？（输入y/n）：");
                    Thread.Sleep(800);
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        if (target.WarriorType == Warrior.赵云 && !target.HandCards.Contains(CardType.闪) && target.HandCards.Contains(CardType.杀))
                        {
                            target.LongDan(CardType.闪);
                        }
                        else
                        {
                            target.HandCards.Remove(CardType.闪);
                        }
                        Console.WriteLine($"{target.Name} 抵消了【杀】！");
                        Thread.Sleep(800);
                        isDodged = true;
                    }
                }

                if (isDodged && currentPlayer.CurrentWeapon?.Card == CardType.青龙偃月刀)
                {
                    Console.Write("青龙偃月刀生效！是否再出1张【杀】攻击同一目标？（y/n）：");
                    Thread.Sleep(800);
                    if (Console.ReadLine()?.ToLower() == "y" && currentPlayer.HandCards.Contains(CardType.杀))
                    {
                        currentPlayer.HandCards.Remove(CardType.杀);
                        Console.WriteLine($"{currentPlayer.Name} 再出1张【杀】！");
                        Thread.Sleep(800);
                        target.HP--;
                        Console.WriteLine($"{target.Name} 受到1点伤害，当前体力：{target.HP}");
                        Thread.Sleep(800);
                        HandleDamage(target, CardType.杀);
                    }
                    continue;
                }

                if (!isDodged)
                {
                    target.HP--;
                    Console.WriteLine($"{target.Name} 受到1点伤害，当前体力：{target.HP}");
                    Thread.Sleep(800);
                    HandleDamage(target, CardType.杀);
                }
            }
        }

        // 使用丈八蛇矛
        static bool UseZhangBaSheMao(Player currentPlayer)
        {
            if (currentPlayer.CurrentWeapon?.Card != CardType.丈八蛇矛 || currentPlayer.HandCards.Count < 2)
            {
                return false;
            }

            Console.Write("是否发动丈八蛇矛？（用2张手牌当作【杀】使用，y/n）：");
            Thread.Sleep(800);
            if (Console.ReadLine()?.ToLower() != "y")
            {
                return false;
            }

            Console.WriteLine($"你的手牌：{string.Join("、", currentPlayer.HandCards.Select((c, i) => $"{i + 1}.{c}"))}");
            Thread.Sleep(800);
            Console.Write("选择2张手牌序号（用逗号分隔）：");
            var indices = Console.ReadLine()?.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int idx) ? idx - 1 : -1)
                .Where(idx => idx >= 0 && idx < currentPlayer.HandCards.Count)
                .Distinct()
                .Take(2)
                .ToList();

            if (indices.Count != 2)
            {
                Console.WriteLine("选择无效，无法发动丈八蛇矛！");
                Thread.Sleep(800);
                return false;
            }

            foreach (var idx in indices.OrderByDescending(i => i))
            {
                currentPlayer.HandCards.RemoveAt(idx);
            }
            Console.WriteLine($"{currentPlayer.Name} 发动丈八蛇矛，将2张手牌当作【杀】使用！");
            Thread.Sleep(800);
            return true;
        }

        // 伤害处理
        static void HandleDamage(Player target, CardType damageCard)
        {
            if (!target.IsAlive)
            {
                Console.WriteLine($"{target.Name} 阵亡了！");
                Thread.Sleep(800);
                return;
            }

            if (target.WarriorType == Warrior.曹操)
            {
                target.JianXiong(damageCard);
            }

            if (target.HandCards.Contains(CardType.桃))
            {
                Console.Write($"{target.Name} 是否使用【桃】回复体力？（y/n）：");
                Thread.Sleep(800);
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    target.UsePeach();
                }
            }
        }

        // 获取攻击范围内目标
        static List<Player> GetValidTargetsByRange(Player currentPlayer, int attackRange)
        {
            var currentIndex = players.IndexOf(currentPlayer);
            return players.Where(p =>
                p.IsAlive && p != currentPlayer &&
                Math.Min(Math.Abs(players.IndexOf(p) - currentIndex), players.Count - Math.Abs(players.IndexOf(p) - currentIndex)) <= attackRange
            ).ToList();
        }

        // 处理过河拆桥
        static void HandleHeGuoChaiQiao(Player currentPlayer)
        {
            var targets = players.Where(p => p != currentPlayer && p.IsAlive && p.HandCards.Count > 0).ToList();
            if (targets.Count == 0)
            {
                Console.WriteLine("无有效目标（其他玩家无手牌），【过河拆桥】使用失败！");
                Thread.Sleep(800);
                return;
            }

            Console.Write($"选择拆桥目标（{string.Join("、", targets.Select(t => t.Name))}）：");
            Thread.Sleep(800);
            var targetName = Console.ReadLine();
            var target = targets.FirstOrDefault(t => t.Name == targetName);
            if (target == null)
            {
                Console.WriteLine("目标无效，【过河拆桥】使用失败！");
                Thread.Sleep(800);
                return;
            }

            Console.WriteLine($"{target.Name} 手牌数：{target.HandCards.Count}张");
            Thread.Sleep(800);
            Console.Write("选择要弃置的手牌序号（1-{target.HandCards.Count}）：");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int idx) && idx >= 1 && idx <= target.HandCards.Count)
            {
                var discardedCard = target.HandCards[idx - 1];
                target.HandCards.RemoveAt(idx - 1);
                Console.WriteLine($"{currentPlayer.Name} 拆弃了 {target.Name} 的1张牌！");
                Thread.Sleep(800);
            }
            else
            {
                Console.WriteLine("选择无效，【过河拆桥】使用失败！");
                Thread.Sleep(800);
            }
        }

        // 处理顺手牵羊
        static void HandleShunShouQianYang(Player currentPlayer)
        {
            var targets = players.Where(p => p != currentPlayer && p.IsAlive && p.HandCards.Count > 0).ToList();
            if (targets.Count == 0)
            {
                Console.WriteLine("无有效目标（其他玩家无手牌），【顺手牵羊】使用失败！");
                Thread.Sleep(800);
                return;
            }

            Console.Write($"选择牵羊目标（{string.Join("、", targets.Select(t => t.Name))}）：");
            Thread.Sleep(800);
            var targetName = Console.ReadLine();
            var target = targets.FirstOrDefault(t => t.Name == targetName);
            if (target == null)
            {
                Console.WriteLine("目标无效，【顺手牵羊】使用失败！");
                Thread.Sleep(800);
                return;
            }

            Console.WriteLine($"{target.Name} 手牌数：{target.HandCards.Count}张");
            Thread.Sleep(800);
            Console.Write("选择要获得的手牌序号（1-{target.HandCards.Count}）：");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int idx) && idx >= 1 && idx <= target.HandCards.Count)
            {
                var stolenCard = target.HandCards[idx - 1];
                target.HandCards.RemoveAt(idx - 1);
                currentPlayer.HandCards.Add(stolenCard);
                Console.WriteLine($"{currentPlayer.Name} 从 {target.Name} 处获得了1张牌！");
                Thread.Sleep(800);
                Console.WriteLine($"当前手牌：{string.Join("、", currentPlayer.HandCards)}");
                Thread.Sleep(800);
            }
            else
            {
                Console.WriteLine("选择无效，【顺手牵羊】使用失败！");
                Thread.Sleep(800);
            }
        }

        // 处理桃园结义
        static void HandleTaoYuanJieYi(Player currentPlayer)
        {
            Console.WriteLine("【桃园结义】生效！所有存活玩家可出【桃】回复1点体力（体力未满时）");
            Thread.Sleep(800);
            foreach (var player in players.Where(p => p.IsAlive))
            {
                if (player.HP >= 4)
                {
                    Console.WriteLine($"{player.Name} 体力已满，无需出桃！");
                    Thread.Sleep(800);
                    continue;
                }

                if (player.HandCards.Contains(CardType.桃))
                {
                    if (player.IsHumanPlayer)
                    {
                        Console.Write($"{player.Name} 是否出【桃】回复体力？（输入y/n）：");
                        Thread.Sleep(800);
                        if (Console.ReadLine()?.ToLower() == "y")
                        {
                            player.UsePeach();
                        }
                        else
                        {
                            Console.WriteLine($"{player.Name} 选择不出桃！");
                            Thread.Sleep(800);
                        }
                    }
                    else
                    {
                        // AI自动出桃回血（困难难度必出，普通50%，简单20%）
                        bool usePeach = false;
                        if (player.AIDiff == AIDifficulty.困难)
                        {
                            usePeach = true;
                        }
                        else if (player.AIDiff == AIDifficulty.普通)
                        {
                            usePeach = new Random().Next(2) == 0;
                        }
                        else
                        {
                            usePeach = new Random().Next(5) == 0;
                        }

                        if (usePeach)
                        {
                            player.UsePeach();
                        }
                        else
                        {
                            Console.WriteLine($"{player.Name} 选择不出桃！");
                            Thread.Sleep(800);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{player.Name} 没有【桃】，无法回复体力！");
                    Thread.Sleep(800);
                }
            }
        }

        // 处理南蛮入侵
        static void HandleNanManRuQin(Player currentPlayer)
        {
            Console.WriteLine("【南蛮入侵】生效！所有存活玩家需出【杀】，否则扣1血！");
            Thread.Sleep(800);
            foreach (var player in players.Where(p => p.IsAlive && p != currentPlayer))
            {
                bool hasSha = player.HandCards.Contains(CardType.杀);
                bool useWuSheng = false;
                bool useZhangBa = false;

                if (!hasSha)
                {
                    if (player.WarriorType == Warrior.关羽 && player.HandCards.Count > 0)
                    {
                        if (player.IsHumanPlayer)
                        {
                            Console.Write($"{player.Name} 是否发动【武圣】抵消南蛮？（输入y/n）：");
                            Thread.Sleep(800);
                            if (Console.ReadLine()?.ToLower() == "y")
                            {
                                CardType usedCard;
                                player.WuSheng(out usedCard);
                                useWuSheng = true;
                                hasSha = true;
                            }
                        }
                        else if (player.AIDiff != AIDifficulty.简单)
                        {
                            CardType usedCard;
                            player.WuSheng(out usedCard);
                            useWuSheng = true;
                            hasSha = true;
                        }
                    }
                    else if (player.CurrentWeapon?.Card == CardType.丈八蛇矛)
                    {
                        useZhangBa = UseZhangBaSheMao(player);
                        hasSha = useZhangBa;
                    }
                }

                if (hasSha || useWuSheng || useZhangBa)
                {
                    if (!useWuSheng && !useZhangBa) player.HandCards.Remove(CardType.杀);
                    Console.WriteLine($"{player.Name} 抵消了南蛮入侵！");
                    Thread.Sleep(800);
                }
                else
                {
                    player.HP--;
                    Console.WriteLine($"{player.Name} 无法出杀，受到1点伤害，当前体力：{player.HP}");
                    Thread.Sleep(800);
                    HandleDamage(player, CardType.南蛮入侵);
                }
            }
        }

        // 处理万箭齐发
        static void HandleWanJianQiFa(Player currentPlayer)
        {
            Console.WriteLine("【万箭齐发】生效！所有存活玩家需出【闪】，否则扣1血！");
            Thread.Sleep(800);
            foreach (var player in players.Where(p => p.IsAlive && p != currentPlayer))
            {
                bool hasShan = player.HandCards.Contains(CardType.闪);
                bool useLongDan = false;

                if (!hasShan && player.WarriorType == Warrior.赵云 && player.HandCards.Contains(CardType.杀))
                {
                    if (player.IsHumanPlayer)
                    {
                        player.LongDan(CardType.闪);
                        useLongDan = true;
                        hasShan = true;
                    }
                    else if (player.AIDiff != AIDifficulty.简单)
                    {
                        player.LongDan(CardType.闪);
                        useLongDan = true;
                        hasShan = true;
                    }
                }

                if (hasShan || useLongDan)
                {
                    if (!useLongDan) player.HandCards.Remove(CardType.闪);
                    Console.WriteLine($"{player.Name} 出【闪】抵消了万箭齐发！");
                    Thread.Sleep(800);
                }
                else
                {
                    player.HP--;
                    Console.WriteLine($"{player.Name} 无法出闪，受到1点伤害，当前体力：{player.HP}");
                    Thread.Sleep(800);
                    HandleDamage(player, CardType.万箭齐发);
                }
            }
        }

        // 处理无中生有
        static void HandleWuZhongShengYou(Player currentPlayer)
        {
            DrawCards(currentPlayer, 2);
            Console.WriteLine($"{currentPlayer.Name} 摸了2张牌！");
            Thread.Sleep(800);
            if (currentPlayer.IsHumanPlayer)
            {
                Console.WriteLine($"当前手牌：{string.Join("、", currentPlayer.HandCards)}");
                Thread.Sleep(800);
            }
            else
            {
                Console.WriteLine($"当前手牌：******（共{currentPlayer.HandCards.Count}张）");
                Thread.Sleep(800);
            }
        }

        // 切换玩家
        static void NextPlayer()
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            Console.WriteLine("\n===== 切换回合 =====");
            Thread.Sleep(1000);
        }

        // 游戏存档（保存到savegame.txt）
        static void SaveGame()
        {
            using (StreamWriter sw = new StreamWriter("savegame.txt"))
            {
                // 保存当前玩家索引
                sw.WriteLine(currentPlayerIndex);

                // 保存牌堆
                sw.WriteLine(string.Join(",", cardDeck));

                // 保存玩家信息
                sw.WriteLine(players.Count);
                foreach (var p in players)
                {
                    sw.WriteLine($"{p.Name},{p.WarriorType},{p.HP},{p.IsAlive},{p.AIDiff}");
                    sw.WriteLine(string.Join(",", p.HandCards));
                    sw.WriteLine(p.CurrentWeapon?.Card.ToString() ?? "无");
                    sw.WriteLine(p.CurrentArmor?.Card.ToString() ?? "无");
                }
            }
        }

        // 读取存档
        static void LoadGame()
        {
            using (StreamReader sr = new StreamReader("savegame.txt"))
            {
                // 读取当前玩家索引
                currentPlayerIndex = int.Parse(sr.ReadLine());

                // 读取牌堆
                var deckStr = sr.ReadLine();
                cardDeck = new Queue<CardType>(deckStr.Split(',').Select(s => (CardType)Enum.Parse(typeof(CardType), s)));

                // 读取玩家信息
                int playerCount = int.Parse(sr.ReadLine());
                players.Clear();
                for (int i = 0; i < playerCount; i++)
                {
                    var playerInfo = sr.ReadLine().Split(',');
                    var p = new Player
                    {
                        Name = playerInfo[0],
                        WarriorType = (Warrior)Enum.Parse(typeof(Warrior), playerInfo[1]),
                        HP = int.Parse(playerInfo[2]),
                        AIDiff = (AIDifficulty)Enum.Parse(typeof(AIDifficulty), playerInfo[4])
                    };

                    // 读取手牌
                    var handCardsStr = sr.ReadLine();
                    p.HandCards = handCardsStr.Split(',').Select(s => (CardType)Enum.Parse(typeof(CardType), s)).ToList();

                    // 读取装备
                    var weaponStr = sr.ReadLine();
                    if (weaponStr != "无" && EquipmentConfig.ContainsKey((CardType)Enum.Parse(typeof(CardType), weaponStr)))
                    {
                        p.CurrentWeapon = EquipmentConfig[(CardType)Enum.Parse(typeof(CardType), weaponStr)];
                    }

                    var armorStr = sr.ReadLine();
                    if (armorStr != "无" && EquipmentConfig.ContainsKey((CardType)Enum.Parse(typeof(CardType), armorStr)))
                    {
                        p.CurrentArmor = EquipmentConfig[(CardType)Enum.Parse(typeof(CardType), armorStr)];
                    }

                    players.Add(p);
                }
            }
        }
    }
}