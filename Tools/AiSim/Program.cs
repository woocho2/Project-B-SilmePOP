using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

// 게임의 순수 C# 코드(판, 스킬, AI)로 120초 판을 그대로 돌림. Unity 없이.
public static class Program
{
    public sealed class Result
    {
        public int Score, Moves, SkillUses, SkillScore, Removed;
        public float StuckTime, ClearedAt = -1f;
        public double MaxActMs;
        public Dictionary<SkillType, int> Skills = new Dictionary<SkillType, int>();
    }

    public static Action<BoardModel> BoardHook;   // 판 규칙 실험용 (악마 빼기 등)

    // 악마 실험: 이번 판에서 악마가 든 조합을 몇 번 터뜨렸는지
    public static int DevilPops;

    public static int TargetOverride;   // 0이 아니면 같은 판을 이 목표 합으로
    public static float PerSlimeOverride = -1f;
    public static List<int>[] MoveSizes;   // 난이도별 수 하나에 쓴 슬라임 수 (seed 모드)

    public static Result Play(int seed, AiDifficulty difficulty, AiStrategy strategy, float minR = -1, float maxR = -1, int agentSeed = 1)
    {
        MatchSettings settings = TargetOverride > 0 ? new MatchSettings(seed, TargetOverride, 0L, 1) : MatchSettings.CreateNew(0L, 1, seed);
        BoardModel board = BoardGenerator.Generate(seed);
        BoardHook?.Invoke(board);
        SkillSet skills = new SkillSet(seed, settings.Skills);
        AiAgent agent = new AiAgent(difficulty, unchecked(seed * 397 + agentSeed), strategy);
        if (minR > 0) { agent.MinReaction = minR; agent.MaxReaction = maxR; }
        if (PerSlimeOverride >= 0) agent.PerSlimeDelay = PerSlimeOverride;

        Result r = new Result();
        const float dt = 0.05f;
        float t = 0f, wait = agent.FirstWait(settings.TargetSum);
        Stopwatch sw = new Stopwatch();
        while (t < GameRules.MatchDuration)
        {
            board.Tick(dt);
            t += dt;
            wait -= dt;
            if (wait > 0f) continue;

            sw.Restart();
            AiAction a = agent.Act(board, skills, settings.TargetSum, GameRules.MatchDuration - t);
            r.MaxActMs = Math.Max(r.MaxActMs, sw.Elapsed.TotalMilliseconds);
            lock (ActTimes) ActTimes.Add(sw.Elapsed.TotalMilliseconds);
            r.Score += a.Score;
            wait = a.Wait;
            if (a.Kind == AiActionKind.Move)
            {
                r.Moves++;
                if (MoveSizes != null) lock (MoveSizes) MoveSizes[(int)difficulty].Add(a.SlimeCount);
            }
            else if (a.Kind == AiActionKind.Skill)
            {
                r.SkillUses++;
                r.SkillScore += a.Score;
                r.Skills[a.Skill] = r.Skills.TryGetValue(a.Skill, out int n) ? n + 1 : 1;
            }
            else r.StuckTime += a.Wait;
        }
        for (int c = 0; c < GameRules.CellCount; c++)
        {
            if (board.GetSlimeAt(c) == null) r.Removed++;   // 비워진 칸 = 없앤 슬라임 (합체 슬라임은 4칸)
        }
        return r;
    }

    public static Result[] PlayMany(IEnumerable<int> seeds, AiDifficulty d, AiStrategy s, float minR = -1, float maxR = -1)
    {
        int[] arr = seeds.ToArray();
        Result[] results = new Result[arr.Length];
        Parallel.For(0, arr.Length, i => results[i] = Play(arr[i], d, s, minR, maxR));
        return results;
    }

    static string Summary(Result[] rs)
    {
        double avg = rs.Average(r => r.Score);
        double sd = Math.Sqrt(rs.Average(r => (r.Score - avg) * (r.Score - avg)));
        var skillCounts = new Dictionary<SkillType, int>();
        foreach (var r in rs) foreach (var kv in r.Skills) skillCounts[kv.Key] = (skillCounts.TryGetValue(kv.Key, out int n) ? n : 0) + kv.Value;
        string skills = string.Join(" ", skillCounts.OrderBy(k => k.Key).Select(k => $"{k.Key}:{(double)k.Value / rs.Length:0.00}"));
        double rem = rs.Average(r => r.Removed);
        return $"removed {rem,5:0.0} ({rs.Min(r => r.Removed)}~{rs.Max(r => r.Removed)}) | score {avg,6:0.0} sd {sd,5:0.0} | moves {rs.Average(r => r.Moves),5:0.0} | items/game {rs.Average(r => r.SkillUses):0.00} itemPts {rs.Average(r => r.SkillScore):0.0} | stuck {rs.Average(r => r.StuckTime),5:0.0}s | maxAct {rs.Max(r => r.MaxActMs):0}ms | {skills}";
    }

    public static void Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "bench";
        if (mode == "bench")
        {
            int n = args.Length > 1 ? int.Parse(args[1]) : 20;
            foreach (AiDifficulty d in new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell })
            {
                Stopwatch sw = Stopwatch.StartNew();
                Result[] rs = PlayMany(Enumerable.Range(1000, n), d, AiStrategy.Best);
                Console.WriteLine($"{d,-6} {Summary(rs)}  ({sw.Elapsed.TotalSeconds:0.0}s)");
            }
        }
        else if (mode == "records")
        {
            // 직접 둔 판(아이템 1개 판만)을 지금 AI 설정으로 다시: 난이도별 평균 제거와 내 제거 수가 이긴 비율
            var ds = new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell };
            var rows = System.IO.File.ReadAllLines(args[1]).Skip(1).Select(l => l.Split(',')).Where(c => c.Length > 5 && !c[3].Contains('/')).ToList();
            var sumAvg = new double[4]; var sumWin = new double[4];
            foreach (var c in rows)
            {
                int seed = int.Parse(c[1]), target = int.Parse(c[2]), mine = int.Parse(c[5]);
                TargetOverride = target;
                var parts = new List<string>();
                for (int k = 0; k < 4; k++)
                {
                    Result[] rs = new Result[200];
                    Parallel.For(0, 200, i => rs[i] = Play(seed, ds[k], AiStrategy.Best, -1, -1, i + 1));
                    double avg = rs.Average(r => r.Removed);
                    double win = rs.Average(r => mine > r.Removed ? 1 : mine == r.Removed ? 0.5 : 0);
                    sumAvg[k] += avg; sumWin[k] += win;
                    parts.Add($"{ds[k]} {avg,5:0.0} ({win,4:P0})");
                }
                Console.WriteLine($"목표 {target,2} 나 {mine,3}마리 | {string.Join(" | ", parts)}");
            }
            TargetOverride = 0;
            Console.WriteLine($"평균 ({rows.Count}판) 나 {rows.Average(c => int.Parse(c[5])):0.0}마리 | " +
                string.Join(" | ", Enumerable.Range(0, 4).Select(k => $"{ds[k]} {sumAvg[k] / rows.Count,5:0.0} ({sumWin[k] / rows.Count,4:P0})")));
        }
        else if (mode == "games" || mode == "fit")
        {
            // player_games.csv: 목표 2, 시드 4, 제거 6
            var ds = new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell };
            var rows = System.IO.File.ReadAllLines(args[1]).Skip(1).Select(l => l.Split(','))
                .Select(c => (target: int.Parse(c[2]), seed: int.Parse(c[4]), mine: int.Parse(c[6]))).ToList();
            foreach (var group in rows.GroupBy(r => r.target).OrderBy(g => g.Key))
            {
                var games = group.ToList();
                TargetOverride = group.Key;
                double me = games.Average(g => g.mine);
                Console.WriteLine($"== 목표 {group.Key}: 나 평균 {me:0.0}마리 ({games.Count}판)");
                if (mode == "games")
                {
                    foreach (var d in ds)
                    {
                        double avg = 0, win = 0;
                        foreach (var g in games)
                        {
                            Result[] rs = new Result[100];
                            Parallel.For(0, 100, i => rs[i] = Play(g.seed, d, AiStrategy.Best, -1, -1, i + 1));
                            avg += rs.Average(r => r.Removed);
                            win += rs.Average(r => g.mine > r.Removed ? 1 : g.mine == r.Removed ? 0.5 : 0);
                        }
                        Console.WriteLine($"   {d,-6} AI {avg / games.Count,5:0.0}마리 | 내가 이긴 비율 {win / games.Count,4:P0}");
                    }
                }
                else
                {
                    // 난이도(정확도)마다, 내 평균의 몇 %를 지우게 하려면 반응 시간이 몇 초여야 하나 (최대 = 최소 × 2, 이 목표 합에서 실제로 걸리는 시간)
                    foreach (var (d, ratio) in new[] { (AiDifficulty.Easy, 0.6), (AiDifficulty.Normal, 0.8), (AiDifficulty.Hard, 0.95), (AiDifficulty.Hard, 1.0) })
                    {
                        float scale = AiDifficultyTable.ReactionScale(d, group.Key);
                        double want = me * ratio, lo = 0.3, hi = 30;
                        for (int it = 0; it < 14; it++)
                        {
                            double mid = (lo + hi) / 2;
                            double avg = games.Average(g =>
                            {
                                Result[] rs = new Result[40];
                                Parallel.For(0, 40, i => rs[i] = Play(g.seed, d, AiStrategy.Best, (float)(mid / scale), (float)(mid * 2 / scale), i + 1));
                                return rs.Average(r => r.Removed);
                            });
                            if (avg > want) lo = mid; else hi = mid;
                        }
                        double m = (lo + hi) / 2;
                        Console.WriteLine($"   {d,-6} 내 평균의 {ratio,4:P0} ({want,5:0.0}마리) → 반응 {m:0.00} ~ {m * 2:0.00}초");
                    }
                }
            }
            TargetOverride = 0;
        }
        else if (mode == "target")
        {
            // 목표 합별 (같은 판들을 목표만 바꿔서) 난이도별 제거/점수
            int n = args.Length > 1 ? int.Parse(args[1]) : 200;
            int[] seeds = Enumerable.Range(1000, n).ToArray();
            foreach (AiDifficulty d in new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell })
            {
                var parts = new List<string>();
                for (int target = 10; target <= 15; target++)
                {
                    TargetOverride = target;
                    Result[] rs = PlayMany(seeds, d, AiStrategy.Best);
                    parts.Add($"{target}: {rs.Average(r => r.Removed),5:0.0}마리 {rs.Average(r => r.Score),5:0}점");
                }
                Console.WriteLine($"{d,-6} {string.Join(" | ", parts)}");
            }
            TargetOverride = 0;
        }
        else if (mode == "search") Search(int.Parse(args[1]), int.Parse(args[2]));
        else if (mode == "calib") Calibrate(args.Length > 1 ? int.Parse(args[1]) : 100);
        else if (mode == "items") Items();
        else if (mode == "perslime")
        {
            // 큰 조합 지연 k마다: 목표 10에서 보통 AI가 90마리가 되게 반응 시간을 맞춘 뒤, 목표 10~15에서 몇 마리 지우는지
            int[] seeds = Enumerable.Range(80000, 150).ToArray();
            foreach (float k in new[] { 0.08f, 0.5f, 1.0f, 1.5f, 2.0f })
            {
                PerSlimeOverride = k;
                TargetOverride = 10;
                double lo = 0.2, hi = 10;
                for (int it = 0; it < 12; it++)
                {
                    double mid = (lo + hi) / 2;
                    double avg = PlayMany(seeds, AiDifficulty.Normal, AiStrategy.Best, (float)mid, (float)(mid * 2)).Average(r => r.Removed);
                    if (avg > 90) lo = mid; else hi = mid;
                }
                double m = (lo + hi) / 2;
                StringBuilderLine($"k={k:0.00}s  보통 반응 {m:0.00}~{m * 2:0.00}s  →", seeds, (float)m);
            }
            PerSlimeOverride = -1f;
            TargetOverride = 0;
        }
        else if (mode == "seed")
        {
            int seed = int.Parse(args[1]);
            var ds = new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell };

            // 1) 이 판 그대로 (200판씩, AI 난수만 바꿔서)
            MoveSizes = new[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
            Console.WriteLine($"== 시드 {seed}, 목표 {MatchSettings.CreateNew(0L, 1, seed).TargetSum} (실제 판)");
            foreach (var d in ds)
            {
                Result[] rs = new Result[200];
                Parallel.For(0, 200, i => rs[i] = Play(seed, d, AiStrategy.Best, -1, -1, i + 1));
                var sizes = MoveSizes[(int)d];
                string mix = string.Join(" ", sizes.GroupBy(x => x).OrderBy(g => g.Key).Select(g => $"{g.Key}마리 {100.0 * g.Count() / sizes.Count:0}%"));
                Console.WriteLine($"{d,-6} {Summary(rs)}");
                Console.WriteLine($"       수 하나의 크기: {mix}");
            }

            // 2) 같은 판을 목표 합만 바꿔서: 판 자체가 얼마나 어려운지 (가능한 조합 수, 조합 크기)
            Console.WriteLine();
            Console.WriteLine("== 같은 판, 목표 합만 바꾸면 (처음 판에서 가능한 조합 수 / 그중 2마리짜리 비율, 보통 AI 평균)");
            BoardModel fresh = BoardGenerator.Generate(seed);
            for (int target = 10; target <= 15; target++)
            {
                var all = MatchFinder.FindAll(fresh, target);
                int pairs = all.Count(r => fresh.GetSlimesInCells(r.GetCells()).Count == 2);
                TargetOverride = target;
                Result[] rs = new Result[200];
                Parallel.For(0, 200, i => rs[i] = Play(seed, AiDifficulty.Normal, AiStrategy.Best, -1, -1, i + 1));
                Console.WriteLine($"목표 {target}: 조합 {all.Count,3}개 (2마리 {100.0 * pairs / Math.Max(1, all.Count),3:0}%) | 보통 AI 제거 {rs.Average(r => r.Removed),5:0.0} 점수 {rs.Average(r => r.Score),5:0.0}");
            }
            TargetOverride = 0;
            MoveSizes = null;
        }
        else if (mode == "devil")
        {
            int[] seeds = Enumerable.Range(60000, 300).ToArray();
            var variants = new (string name, Action<BoardModel> hook)[]
            {
                ("악마 5 (지금)", null),
                ("악마 0 (전부 기본으로)", b => ForEachSlime(b, s => { if (s.Type == SlimeType.Devil) s.Type = SlimeType.Basic; })),
                ("악마 15 (기본 10마리 더 악마로)", b => { int n = 0; ForEachSlime(b, s => { if (s.Type == SlimeType.Basic && n < 10 && s.Id % 7 == 0) { s.Type = SlimeType.Devil; n++; } }); }),
            };
            foreach (AiDifficulty d in new[] { AiDifficulty.Easy, AiDifficulty.Hard, AiDifficulty.Hell })
            {
                foreach (var v in variants)
                {
                    BoardHook = v.hook;
                    Result[] rs = PlayMany(seeds, d, AiStrategy.Best);
                    Console.WriteLine($"{d,-6} {v.name,-28} removed {rs.Average(r => r.Removed),5:0.0} | score {rs.Average(r => r.Score),6:0.0}");
                }
            }
            BoardHook = null;

            // 판이 끝났을 때 악마가 몇 마리 남는지 (지금 규칙, 지옥/어려움)
            foreach (AiDifficulty d in new[] { AiDifficulty.Easy, AiDifficulty.Hard, AiDifficulty.Hell })
            {
                double left = 0;
                foreach (int seed in seeds.Take(100)) left += DevilsLeft(seed, d);
                Console.WriteLine($"{d,-6} 판 끝에 남은 악마: 5마리 중 평균 {left / 100:0.0}마리");
            }
        }
        else if (mode == "repeat")
        {
            // 같은 판(시드 고정)을 AI 난수만 바꿔 30번씩: 점수가 얼마나 흔들리는지
            foreach (int board in new[] { 1001, 1002, 1003, 1004, 1005 })
            {
                Console.Write($"board {board}: ");
                foreach (AiDifficulty d in new[] { AiDifficulty.Easy, AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell })
                {
                    var scores = Enumerable.Range(1, 30).Select(a => Play(board, d, AiStrategy.Best, -1, -1, a).Score).ToList();
                    var removed = Enumerable.Range(1, 30).Select(a => Play(board, d, AiStrategy.Best, -1, -1, a).Removed).ToList();
                    Console.Write($"{d} {scores.Min()}~{scores.Max()}점 (서로 다른 점수 {scores.Distinct().Count()}종, 제거 {removed.Min()}~{removed.Max()})  ");
                }
                Console.WriteLine();
            }
        }
        else if (mode == "timing")
        {
            PlayMany(Enumerable.Range(1, 20), AiDifficulty.Hard, AiStrategy.Best);   // JIT 데우기
            foreach (AiDifficulty d in new[] { AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell })
            {
                ActTimes.Clear();
                foreach (int seed in Enumerable.Range(3000, 100)) Play(seed, d, AiStrategy.Best);   // 한 줄로 (병렬 간섭 없이)
                var t = ActTimes.OrderBy(x => x).ToList();
                Console.WriteLine($"{d,-6} acts {t.Count} avg {t.Average():0.00}ms p50 {t[t.Count / 2]:0.00} p99 {t[(int)(t.Count * 0.99)]:0.00} p999 {t[(int)(t.Count * 0.999)]:0.00} max {t.Last():0.00}");
            }
        }
    }

    static readonly List<double> ActTimes = new List<double>();

    static void StringBuilderLine(string head, int[] seeds, float minR)
    {
        var parts = new List<string>();
        for (int target = 10; target <= 15; target++)
        {
            TargetOverride = target;
            parts.Add($"{target}:{PlayMany(seeds, AiDifficulty.Normal, AiStrategy.Best, minR, minR * 2).Average(r => r.Removed),5:0.0}");
        }
        TargetOverride = 10;
        Console.WriteLine($"{head} 목표별 제거  {string.Join("  ", parts)}");
    }

    static void ForEachSlime(BoardModel b, Action<BoardSlime> act)
    {
        var seen = new HashSet<BoardSlime>();
        for (int c = 0; c < GameRules.CellCount; c++) { var s = b.GetSlimeAt(c); if (s != null && seen.Add(s)) act(s); }
    }

    static int DevilsLeft(int seed, AiDifficulty d)
    {
        MatchSettings settings = MatchSettings.CreateNew(0L, 1, seed);
        BoardModel board = BoardGenerator.Generate(seed);
        SkillSet skills = new SkillSet(seed, settings.Skills);
        AiAgent agent = new AiAgent(d, seed);
        float wait = agent.FirstWait(settings.TargetSum);
        for (float t = 0; t < 120f; t += 0.05f)
        {
            board.Tick(0.05f); wait -= 0.05f;
            if (wait <= 0) wait = agent.Act(board, skills, settings.TargetSum, 120f - t).Wait;
        }
        int left = 0;
        ForEachSlime(board, s => { if (s.Type == SlimeType.Devil) left++; });
        return left;
    }

    // 수 고르기는 cw=-3으로 두고, 아이템 기준값만 바꿔 하드·노말(잘 안 막히는 속도)에서 비교
    static void Items()
    {
        int[] seeds = Enumerable.Range(40000, 300).ToArray();
        var sets = new (string name, float joker, float merge, float destroy, float num, float endT, float endF)[]
        {
            ("never-unless-stuck", 999, 999, 999, 999, 0, 1),
            ("eager",               0,   0,   0,  0.5f, 20, 0.6f),
            ("default",            10,   9,   8,   3,   20, 0.6f),
            ("picky",              20,  12,  12,   6,   20, 0.3f),
            ("picky-end30",        20,  12,  12,   6,   30, 0.3f),
            ("mid",                10,  12,   8,   1.5f, 20, 0.6f),
        };
        foreach (AiDifficulty d in new[] { AiDifficulty.Normal, AiDifficulty.Hard, AiDifficulty.Hell })
        {
            foreach (var set in sets)
            {
                AiStrategy s = new AiStrategy
                {
                    CountWeight = -3f, JokerThreshold = set.joker, MergeThreshold = set.merge, DestroyThreshold = set.destroy,
                    NumberChangeThreshold = set.num, EndgameTime = set.endT, EndgameFactor = set.endF
                };
                Console.WriteLine($"{d,-6} {set.name,-20} {Summary(PlayMany(seeds, d, s))}");
            }
        }
    }

    // 지옥 속도·정확도 100%로 전략값을 무작위로 바꿔 가며 평균 점수 비교 (같은 시드 묶음)
    static void Search(int configs, int seedsPer)
    {
        Random rnd = new Random(12345);
        int[] seeds = Enumerable.Range(5000, seedsPer).ToArray();
        var list = new List<(double avg, AiStrategy s, string desc)>();

        AiStrategy baseline = new AiStrategy();
        list.Add(Eval("baseline", baseline));

        for (int i = 0; i < configs - 1; i++)
        {
            AiStrategy s = new AiStrategy
            {
                CountWeight = Pick(rnd, -4f, -3f, -2f, -1.5f, -1f, -0.5f),
                Lookahead = Pick(rnd, 0, 0, 5, 10),
                FutureWeight = Pick(rnd, 0f, 1f, 2f, 4f),
                JokerThreshold = Pick(rnd, 0f, 10f, 20f, 26f),
                MergeThreshold = Pick(rnd, 0f, 5f, 9f, 12f),
                DestroyThreshold = Pick(rnd, 0f, 3f, 8f, 12f, 99f),
                NumberChangeThreshold = Pick(rnd, 0.5f, 1.5f, 3f, 6f, 99f),
                EndgameTime = Pick(rnd, 10f, 20f, 40f),
                EndgameFactor = Pick(rnd, 0f, 0.3f, 0.6f),
            };
            if (s.Lookahead == 0) s.FutureWeight = 0f;
            list.Add(Eval($"#{i}", s));
        }

        Console.WriteLine();
        Console.WriteLine("TOP 8:");
        foreach (var e in list.OrderByDescending(e => e.avg).Take(8)) Console.WriteLine($"{e.avg,6:0.0}  {e.desc}");

        // 상위 6개 + 기본값을 새 시드 200판으로 다시 (운 좋게 뽑힌 것 걸러내기)
        Console.WriteLine();
        Console.WriteLine("VALIDATE (200 fresh seeds):");
        int[] fresh = Enumerable.Range(70000, 200).ToArray();
        foreach (var e in list.OrderByDescending(e => e.avg).Take(6).Append(list[0]))
        {
            Result[] rs = PlayMany(fresh, AiDifficulty.Hell, e.s);
            Console.WriteLine($"{Summary(rs)}  <- {e.desc}");
        }

        (double, AiStrategy, string) Eval(string name, AiStrategy s)
        {
            Result[] rs = PlayMany(seeds, AiDifficulty.Hell, s);
            string desc = $"{name} cw={s.CountWeight} look={s.Lookahead} fw={s.FutureWeight} joker={s.JokerThreshold} merge={s.MergeThreshold} destroy={s.DestroyThreshold} num={s.NumberChangeThreshold} end={s.EndgameTime}x{s.EndgameFactor}";
            Console.WriteLine($"{Summary(rs)}  <- {desc}");
            return (rs.Average(r => r.Score), s, desc);
        }
    }

    static T Pick<T>(Random r, params T[] v) => v[r.Next(v.Length)];

    // 난이도마다 반응 시간 배율을 이분 탐색해 목표 평균 점수에 맞춤 (최소 : 최대 = 1 : 2)
    static void Calibrate(int n)
    {
        var targets = new (AiDifficulty d, double score)[] { (AiDifficulty.Easy, 70), (AiDifficulty.Normal, 90), (AiDifficulty.Hard, 110) };
        int[] seeds = Enumerable.Range(9000, n).ToArray();
        foreach (var (d, target) in targets)
        {
            double lo = 0.2, hi = 10;   // 최소 반응 시간
            for (int it = 0; it < 14; it++)
            {
                double mid = (lo + hi) / 2;
                double avg = PlayMany(seeds, d, AiStrategy.Best, (float)mid, (float)(mid * 2)).Average(r => r.Removed);
                if (avg > target) lo = mid; else hi = mid;
            }
            double m = (lo + hi) / 2;
            Result[] check = PlayMany(Enumerable.Range(20000, n), d, AiStrategy.Best, (float)m, (float)(m * 2));
            Console.WriteLine($"{d,-6} target {target}: reaction {m:0.00} ~ {m * 2:0.00}s -> {Summary(check)}");
        }
        Result[] hell = PlayMany(Enumerable.Range(20000, n), AiDifficulty.Hell, AiStrategy.Best);
        Console.WriteLine($"Hell   table reaction -> {Summary(hell)}");
    }
}
