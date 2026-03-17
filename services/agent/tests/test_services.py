from fastapi.testclient import TestClient

from main import app


class TestHealthEndpoint:
    def test_ヘルスチェックが動作する(self):
        client = TestClient(app)
        response = client.get("/health")
        assert response.status_code == 200
        assert response.json() == {"status": "ok"}
