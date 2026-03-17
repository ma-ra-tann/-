import asyncio
import json
import logging
import os
import re
import time

import anthropic


class ClaudeClient:
    def __init__(self) -> None:
        self._client = anthropic.Anthropic(
            api_key=os.environ.get("ANTHROPIC_API_KEY", "")
        )
        self._model = os.environ.get("CLAUDE_MODEL", "claude-sonnet-4-20250514")
        self._logger: logging.Logger | None = None

    def set_logger(self, logger: logging.Logger) -> None:
        self._logger = logger

    def _log(self, msg: str) -> None:
        if self._logger:
            self._logger.info(msg)

    async def ask(self, prompt: str, _method: str = "ask") -> str:
        return await asyncio.to_thread(self._ask_sync, prompt, _method)

    def _ask_sync(self, prompt: str, _method: str = "ask") -> str:
        self._log(f"[Claude] ===== REQUEST ({_method}) =====")
        self._log(f"[Claude] PROMPT:\n{prompt}")
        self._log(f"[Claude] ===== END PROMPT =====")

        start = time.time()
        message = self._client.messages.create(
            model=self._model,
            max_tokens=4096,
            messages=[{"role": "user", "content": prompt}],
        )
        elapsed = time.time() - start
        response_text = message.content[0].text
        input_tokens = message.usage.input_tokens
        output_tokens = message.usage.output_tokens

        self._log(f"[Claude] ===== RESPONSE ({_method}) {elapsed:.1f}s | in={input_tokens} out={output_tokens} =====")
        self._log(f"[Claude] RESPONSE:\n{response_text}")
        self._log(f"[Claude] ===== END RESPONSE =====")

        return response_text

    async def ask_json(self, prompt: str) -> object:
        """Ask Claude and parse the JSON from the response, handling markdown code blocks."""
        raw = await self.ask(prompt, _method="ask_json")
        return self.extract_json(raw)

    @staticmethod
    def extract_json(text: str) -> object:
        """Extract JSON from text that may contain markdown code blocks and surrounding text."""
        # Try to find ```json ... ``` block first
        match = re.search(r"```(?:json)?\s*\n?(.*?)\n?\s*```", text, re.DOTALL)
        if match:
            return json.loads(match.group(1).strip())

        # Try to find raw JSON array or object
        for pattern in [r"\[.*\]", r"\{.*\}"]:
            match = re.search(pattern, text, re.DOTALL)
            if match:
                try:
                    return json.loads(match.group(0))
                except json.JSONDecodeError:
                    pass

        # Fallback: try to parse each line as JSON
        for line in text.strip().splitlines():
            line = line.strip()
            if line.startswith(("{", "[")):
                try:
                    return json.loads(line)
                except json.JSONDecodeError:
                    continue

        raise ValueError(f"No JSON found in response: {text[:200]}")
