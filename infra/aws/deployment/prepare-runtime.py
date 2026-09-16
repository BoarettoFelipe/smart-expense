"""Render a private Docker raw env file from SSM JSON on stdin. Never print values."""

import json
import os
import sys


def quote_connection_value(value):
    if not isinstance(value, str) or not value or any(c in value for c in "\r\n\0"):
        raise ValueError("Invalid configuration")
    return '"' + value.replace('"', '""') + '"'


def render_runtime(payload, environment):
    if payload.get("InvalidParameters"):
        raise ValueError("Unavailable parameters")
    parameters = payload["Parameters"]
    expected = {environment["DB_PASSWORD_PARAMETER"], environment["JWT_SIGNING_KEY_PARAMETER"]}
    if len(parameters) != 2 or {p["Name"] for p in parameters} != expected:
        raise ValueError("Unexpected parameters")
    if any(p["Type"] != "SecureString" for p in parameters):
        raise ValueError("Unexpected parameter type")
    values = {p["Name"]: p["Value"] for p in parameters}
    password = values[environment["DB_PASSWORD_PARAMETER"]]
    key = values[environment["JWT_SIGNING_KEY_PARAMETER"]]
    quote_connection_value(key)
    if len(key.encode("utf-8")) < 32:
        raise ValueError("Invalid configuration")
    fields = {
        "Host": environment["RDS_HOST"],
        "Port": environment["RDS_PORT"],
        "Database": environment["RDS_DATABASE"],
        "Username": environment["RDS_USERNAME"],
        "Password": password,
        "SSL Mode": "VerifyFull",
        "Root Certificate": "/run/rds-ca.pem",
    }
    connection = ";".join(k + "=" + quote_connection_value(v) for k, v in fields.items())
    return f"ConnectionStrings__DefaultConnection={connection}\nJwt__SigningKey={key}\n"


def main():
    try:
        content = render_runtime(json.load(sys.stdin), os.environ)
        descriptor = os.open(sys.argv[1], os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as output:
            output.write(content)
    except (ValueError, KeyError, TypeError, OSError, IndexError):
        print("Runtime configuration preparation failed; values withheld.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
