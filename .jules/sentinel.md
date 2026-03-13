## 2025-03-12 - Insecure Password Reset Flow (Auth Bypass)
**Vulnerability:** The `ResetPasswordWithOtpAsync` method allowed updating a user's password using only an email and new password, without verifying if an OTP had actually been validated.
**Learning:** Business logic gaps in authentication flows can lead to complete account takeovers even if individual steps (like OTP validation) are implemented.
**Prevention:** Always verify the session state or a "verified" flag in the database before allowing sensitive operations like password changes. Invalidate verification tokens immediately after successful use.

## 2025-03-12 - Missing Rate Limiting on Auth Endpoints
**Vulnerability:** Authentication endpoints were susceptible to brute force and DoS attacks.
**Learning:** Security is multi-layered. Even with secure password hashing, without rate limiting, an attacker can attempt thousands of combinations per second.
**Prevention:** Implement endpoint-level rate limiting for all sensitive routes (Login, Register, OTP).
