using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 게임 화면 패널 (GamePanel에 붙임): 3, 2, 1 카운트다운, 남은 시간, 막혔을 때 안내.
// 목표 합은 뽑기 화면에서 날아온 슬라임(Game_ReadyPanel)이 시간 글자 위에서, 내 점수는 실시간 순위(Game_RankPanel)가 보여 줌.
// 아이템 3칸과 단축키(A, S, D)는 SkillManager가 맡음. 켜고 끄는 것은 UIManager_Game.
// 비워 둔 글자는 이름으로 찾음 (Txt_Number = 카운트다운, Txt_Warning = 안내). 나머지는 없으면 건너뜀.
public class Game_GamePanel : MonoBehaviour
{
    [Header("Start Countdown")]
    [Tooltip("3, 2, 1, GAME START!! 글자. 비우면 이름 Txt_Number")]
    [SerializeField] private TMP_Text txt_countdown;
    [SerializeField, Min(0.1f)] private float countdownInterval = 1f;
    [SerializeField, Min(0.1f)] private float gameStartDuration = 0.7f;

    [Header("선택 사항")]
    [Tooltip("남은 시간 글자. 비우면 이름 Txt_Timer. 목표 점수 슬라임이 이 위로 날아와 앉음")]
    [SerializeField] private TMP_Text txt_timer;
    [Tooltip("막혔을 때 안내 글자. 비우면 이름 Txt_Warning")]
    [SerializeField] private TMP_Text txt_warning;

    [Header("마지막 몇 초")]
    [Tooltip("남은 시간이 이 초 이하면 시간 글자가 빨개지고 1초마다 톡 튐")]
    [SerializeField, Min(0)] private int warningSeconds = 10;
    [Tooltip("이 초 이하부터는 째깍 소리도")]
    [SerializeField, Min(0)] private int tickSoundSeconds = 5;
    [SerializeField] private Color warningColor = new Color(0.95f, 0.2f, 0.2f);

    [Header("TIME UP")]
    [SerializeField] private string timeUpText = "TIME UP!";
    [Tooltip("TIME UP 글자를 보여 주는 시간 (그 뒤 결과 화면)")]
    [SerializeField, Min(0f)] private float timeUpHold = 1.2f;

    private Sequence countdownTween;
    private Sequence warningTween;
    private Vector2 warningHomePosition;
    private Color timerColor;
    private Vector3 countdownScale = Vector3.one;

    // 목표 점수 슬라임이 앉을 자리 (그 위)
    public RectTransform TimerRect => txt_timer != null ? txt_timer.rectTransform : null;

    private void Awake()
    {
        if (txt_countdown == null) txt_countdown = FindChild("Txt_Number");
        if (txt_warning == null) txt_warning = FindChild("Txt_Warning");
        if (txt_timer == null) txt_timer = FindChild("Txt_Timer");

        if (txt_countdown != null)
        {
            countdownScale = txt_countdown.rectTransform.localScale;
            txt_countdown.raycastTarget = false;
            txt_countdown.gameObject.SetActive(false);
        }
        if (txt_timer != null) timerColor = txt_timer.color;
        if (txt_warning != null)
        {
            warningHomePosition = txt_warning.rectTransform.anchoredPosition;
            txt_warning.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        GameManager game = GameManager.Instance;
        if (game == null) return;

        game.OnTimerUpdated += ShowTimer;
        game.NoMoreMoves += OnNoMoreMoves;
        ShowTimer(game.CurrentTime);
    }

    private void OnDestroy()
    {
        countdownTween?.Kill();
        warningTween?.Kill();
        if (txt_timer != null) txt_timer.rectTransform.DOKill();

        GameManager game = GameManager.Instance;
        if (game == null) return;
        game.OnTimerUpdated -= ShowTimer;
        game.NoMoreMoves -= OnNoMoreMoves;
    }

    // 3, 2, 1, GAME START!! 를 보여 준 뒤 onDone. 글자가 없으면 바로 onDone.
    public void PlayCountdown(Action onDone)
    {
        countdownTween?.Kill();
        if (txt_countdown == null)
        {
            onDone?.Invoke();
            return;
        }

        txt_countdown.text = "";
        txt_countdown.gameObject.SetActive(true);
        countdownTween = DOTween.Sequence().SetUpdate(true);
        for (int count = GameRules.CountdownFrom; count >= 1; count--)
        {
            string text = count.ToString();
            countdownTween.AppendCallback(() =>
            {
                txt_countdown.text = text;
                SoundManager.Play(SoundEffect.Countdown);
            });
            countdownTween.AppendInterval(countdownInterval);
        }
        countdownTween.AppendCallback(() =>
        {
            txt_countdown.text = "GAME START!!";
            SoundManager.Play(SoundEffect.GameStart);
        });
        countdownTween.AppendInterval(gameStartDuration);
        countdownTween.OnComplete(() =>
        {
            countdownTween = null;
            txt_countdown.gameObject.SetActive(false);
            onDone?.Invoke();
        });
    }

    // 3, 2, 1이나 TIME UP을 멈춤 (onDone은 부르지 않음)
    public void StopCountdown()
    {
        countdownTween?.Kill();
        countdownTween = null;
        if (txt_countdown == null) return;
        txt_countdown.rectTransform.localScale = countdownScale;
        txt_countdown.gameObject.SetActive(false);
    }

    // 시간 끝: 카운트다운 자리에 TIME UP!이 크게 쿵 찍히고 잠깐 머문 뒤 onDone (결과 화면)
    public void PlayTimeUp(Action onDone)
    {
        StopCountdown();
        if (txt_countdown == null)
        {
            onDone?.Invoke();
            return;
        }

        RectTransform rect = txt_countdown.rectTransform;
        txt_countdown.text = timeUpText;
        txt_countdown.gameObject.SetActive(true);
        rect.localScale = countdownScale * 2.2f;
        countdownTween = DOTween.Sequence().SetUpdate(true)
            .Append(rect.DOScale(countdownScale, 0.35f).SetEase(Ease.OutBack))
            .AppendInterval(timeUpHold)
            .OnComplete(() =>
            {
                countdownTween = null;
                rect.localScale = countdownScale;
                txt_countdown.gameObject.SetActive(false);
                onDone?.Invoke();
            });
    }

    // 마지막 warningSeconds초는 빨갛게, 1초마다 톡. tickSoundSeconds초부터는 째깍 소리도
    private void ShowTimer(float remainingTime)
    {
        if (txt_timer == null) return;

        int seconds = Mathf.CeilToInt(remainingTime);
        txt_timer.text = seconds.ToString();

        GameManager game = GameManager.Instance;
        bool warning = seconds > 0 && seconds <= warningSeconds && game != null && game.IsPlaying;
        txt_timer.color = warning ? warningColor : timerColor;
        if (!warning) return;

        txt_timer.rectTransform.DOKill(true);
        txt_timer.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.6f);
        if (seconds <= tickSoundSeconds) SoundManager.Play(SoundEffect.Countdown);
    }


    // 막혀도 판은 끝나지 않음. 남은 스킬이 있으면 아이템 칸을 반짝여 알려 줌.
    private void OnNoMoreMoves()
    {
        SkillManager skills = SkillManager.Instance;
        if (skills != null && skills.HasAnySkillLeft)
        {
            ShowWarning("더 이상 만들 수 있는 수가 없습니다!\n아이템을 사용하세요");
            skills.HighlightAvailableSkills();
        }
        else
        {
            ShowWarning("더 이상 만들 수 있는 수가 없습니다!");
        }
    }

    // 안내 글자가 떠오르며 나타났다가 더 떠오르며 사라짐
    public void ShowWarning(string message)
    {
        if (txt_warning == null) return;

        const float rise = 30f;          // 한 번에 올라가는 거리 (픽셀)
        const float fadeDuration = 1f;
        const float pauseDuration = 0.8f;

        warningTween?.Kill();
        txt_warning.text = message;
        txt_warning.gameObject.SetActive(true);

        RectTransform rect = txt_warning.rectTransform;
        rect.anchoredPosition = warningHomePosition;
        Color transparent = txt_warning.color;
        transparent.a = 0f;
        txt_warning.color = transparent;

        warningTween = DOTween.Sequence()
            .Append(rect.DOAnchorPos(warningHomePosition + Vector2.up * rise, fadeDuration).SetEase(Ease.Linear))
            .Join(FadeWarning(1f, fadeDuration))
            .AppendInterval(pauseDuration)
            .Append(rect.DOAnchorPos(warningHomePosition + Vector2.up * rise * 2f, fadeDuration).SetEase(Ease.Linear))
            .Join(FadeWarning(0f, fadeDuration))
            .OnComplete(() =>
            {
                txt_warning.gameObject.SetActive(false);
                rect.anchoredPosition = warningHomePosition;
                warningTween = null;
            });
    }

    // TMP 페이드는 DOTween Pro 기능이라 무료 버전의 범용 트윈으로 함.
    private Tween FadeWarning(float alpha, float duration)
    {
        return DOTween.ToAlpha(() => txt_warning.color, color => txt_warning.color = color, alpha, duration).SetEase(Ease.Linear);
    }

    private TMP_Text FindChild(string childName)
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == childName) return text;
        }
        return null;
    }
}
