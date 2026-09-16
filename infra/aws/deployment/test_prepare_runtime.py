import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("runtime", Path(__file__).with_name("prepare-runtime.py"))
runtime = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runtime)


class RuntimeTests(unittest.TestCase):
    def setUp(self):
        self.environment = {
            "DB_PASSWORD_PARAMETER": "/smart-expense/demo/db-password",
            "JWT_SIGNING_KEY_PARAMETER": "/smart-expense/demo/jwt-signing-key",
            "RDS_HOST": "validation.us-east-1.rds.amazonaws.com",
            "RDS_PORT": "5432", "RDS_DATABASE": "smartexpense", "RDS_USERNAME": "demo",
        }
        self.payload = {"Parameters": [
            {"Name": self.environment["DB_PASSWORD_PARAMETER"], "Type": "SecureString", "Value": 'fake;"$=password'},
            {"Name": self.environment["JWT_SIGNING_KEY_PARAMETER"], "Type": "SecureString", "Value": "x" * 32 + '$"'},
        ], "InvalidParameters": []}

    def test_connection_quoting_and_literal_key(self):
        result = runtime.render_runtime(self.payload, self.environment)
        self.assertIn('Password="fake;""$=password"', result)
        self.assertTrue(result.endswith("Jwt__SigningKey=" + self.payload["Parameters"][1]["Value"] + "\n"))
        self.assertEqual(len(result.splitlines()), 2)

    def test_tls_verifies_server_and_mount_path(self):
        result = runtime.render_runtime(self.payload, self.environment)
        self.assertIn('SSL Mode="VerifyFull"', result)
        self.assertIn('Root Certificate="/run/rds-ca.pem"', result)

    def test_missing_parameter_rejected(self):
        self.payload["Parameters"].pop()
        with self.assertRaises(ValueError):
            runtime.render_runtime(self.payload, self.environment)

    def test_unavailable_parameter_rejected(self):
        self.payload["InvalidParameters"] = ["missing"]
        with self.assertRaises(ValueError):
            runtime.render_runtime(self.payload, self.environment)

    def test_plaintext_parameter_rejected(self):
        self.payload["Parameters"][0]["Type"] = "String"
        with self.assertRaises(ValueError):
            runtime.render_runtime(self.payload, self.environment)

    def test_newline_injection_rejected(self):
        for index in (0, 1):
            payload = copy.deepcopy(self.payload)
            payload["Parameters"][index]["Value"] += "\nInjected=bad"
            with self.assertRaises(ValueError):
                runtime.render_runtime(payload, self.environment)

    def test_short_signing_key_rejected(self):
        self.payload["Parameters"][1]["Value"] = "short"
        with self.assertRaises(ValueError):
            runtime.render_runtime(self.payload, self.environment)


if __name__ == "__main__":
    unittest.main()
