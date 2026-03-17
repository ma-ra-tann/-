import logging
import os
import time
from datetime import datetime
from pathlib import Path

_LOG_DIR = Path(__file__).parent / "output"
_loggers: dict[str, logging.Logger] = {}


def get_logger(vc_name: str) -> logging.Logger:
    """VC名ごとにファイルハンドラ付きのloggerを返す。同じVC名なら同じloggerを再利用。"""
    if vc_name in _loggers:
        return _loggers[vc_name]

    _LOG_DIR.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    safe_name = vc_name.replace("/", "_").replace(" ", "_")
    log_file = _LOG_DIR / f"analysis_{safe_name}_{timestamp}.log"

    logger = logging.getLogger(f"analysis.{safe_name}.{timestamp}")
    logger.setLevel(logging.DEBUG)
    logger.propagate = False

    # ファイルハンドラ
    fh = logging.FileHandler(log_file, encoding="utf-8")
    fh.setLevel(logging.DEBUG)
    fh.setFormatter(logging.Formatter("%(asctime)s %(message)s", datefmt="%Y-%m-%d %H:%M:%S"))

    # コンソールにも出力（uvicornターミナルで確認用）
    ch = logging.StreamHandler()
    ch.setLevel(logging.INFO)
    ch.setFormatter(logging.Formatter("%(asctime)s %(message)s", datefmt="%H:%M:%S"))

    logger.addHandler(fh)
    logger.addHandler(ch)

    _loggers[vc_name] = logger
    return logger


def clear_logger(vc_name: str) -> None:
    """分析完了後にloggerをクリーンアップ。"""
    if vc_name in _loggers:
        logger = _loggers.pop(vc_name)
        for handler in logger.handlers[:]:
            handler.close()
            logger.removeHandler(handler)


class Timer:
    """コンテキストマネージャで経過時間を計測。"""

    def __init__(self) -> None:
        self.elapsed: float = 0.0
        self._start: float = 0.0

    def __enter__(self) -> "Timer":
        self._start = time.time()
        return self

    def __exit__(self, *args) -> None:
        self.elapsed = time.time() - self._start
