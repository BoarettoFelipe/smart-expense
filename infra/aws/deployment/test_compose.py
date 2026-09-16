import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


@unittest.skipUnless(shutil.which(os.environ.get("DOCKER_EXE", "docker")), "Docker CLI required")
class ComposeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        directory = tempfile.TemporaryDirectory(prefix="smart-expense-compose-test-")
        cls.addClassCleanup(directory.cleanup)
        runtime_file = Path(directory.name) / "runtime.env"
        cls.key = "x" * 32 + '$"'
        runtime_file.write_text("Jwt__SigningKey=" + cls.key + "\n", encoding="utf-8")
        environment = dict(os.environ, ECR_REGISTRY="123456789012.dkr.ecr.us-east-1.amazonaws.com",
                           IMAGE_TAG="validation", RUNTIME_ENV_FILE=str(runtime_file), RDS_CA_FILE=str(runtime_file))
        result = subprocess.run([os.environ.get("DOCKER_EXE", "docker"), "compose", "--env-file",
                                 str(Path(__file__).with_name("deployment.env.example")), "-f",
                                 str(Path(__file__).with_name("compose.yaml")), "--profile", "tools",
                                 "config", "--format", "json"], env=environment, capture_output=True, text=True)
        if result.returncode:
            raise AssertionError("Compose config validation failed; captured output withheld")
        cls.services = json.loads(result.stdout)["services"]

    def test_no_postgres_service_and_no_public_api_port(self):
        self.assertEqual(set(self.services), {"api", "frontend", "migrations"})
        self.assertFalse(self.services["api"].get("ports"))
        self.assertFalse(self.services["migrations"].get("ports"))
        ports = self.services["frontend"]["ports"]
        self.assertEqual(len(ports), 1)
        self.assertEqual(ports[0]["published"], "80")
        self.assertEqual(ports[0]["target"], 8080)

    def test_raw_env_preserves_literal_characters(self):
        for name in ("api", "migrations"):
            # config serializes '$' as '$$' for a subsequently reusable Compose file.
            # smoke-local.ps1 also checks the actual container environment.
            self.assertEqual(self.services[name]["environment"]["Jwt__SigningKey"], self.key.replace("$", "$$"))

    def test_migrations_are_explicit_and_share_api_repository(self):
        migration = self.services["migrations"]
        self.assertEqual(migration["image"], self.services["api"]["image"] + "-migrations")
        self.assertEqual(migration["profiles"], ["tools"])
        self.assertEqual(migration["logging"]["driver"], "none")

    def test_certificate_read_only_and_api_health_dependency(self):
        for name in ("api", "migrations"):
            mount = self.services[name]["volumes"][0]
            self.assertTrue(mount["read_only"])
            self.assertEqual(mount["target"], "/run/rds-ca.pem")
        self.assertEqual(self.services["frontend"]["depends_on"]["api"]["condition"], "service_healthy")


if __name__ == "__main__":
    unittest.main()
