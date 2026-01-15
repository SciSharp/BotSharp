# Code Review Checklist

Use this checklist during code reviews to ensure comprehensive coverage.

## ✅ Correctness
- [ ] Code implements the intended functionality
- [ ] Logic is correct and handles all scenarios
- [ ] Edge cases are handled properly
- [ ] No off-by-one errors
- [ ] Null/undefined values are handled

## 🔒 Security
- [ ] No SQL injection vulnerabilities
- [ ] No XSS vulnerabilities
- [ ] Input validation is performed
- [ ] Sensitive data is not logged
- [ ] Authentication/authorization is correct
- [ ] No hardcoded secrets or credentials

## ⚡ Performance
- [ ] No unnecessary database queries (N+1 problem)
- [ ] Appropriate data structures used
- [ ] No memory leaks
- [ ] Caching used where appropriate
- [ ] Large operations are async/background

## 🔧 Maintainability
- [ ] Code is readable and self-documenting
- [ ] Functions/methods are focused (single responsibility)
- [ ] No code duplication (DRY)
- [ ] Naming is clear and consistent
- [ ] Complex logic has comments
- [ ] Magic numbers are constants

## 🧪 Testing
- [ ] Unit tests cover main functionality
- [ ] Edge cases have tests
- [ ] Tests are readable and maintainable
- [ ] Mocking is used appropriately
- [ ] Integration tests for critical paths

## 📚 Documentation
- [ ] Public APIs are documented
- [ ] Complex algorithms have explanations
- [ ] README updated if needed
- [ ] Breaking changes documented
- [ ] Migration guide if applicable

## 🎨 Style
- [ ] Follows project coding standards
- [ ] Consistent formatting
- [ ] No commented-out code
- [ ] No debug statements left
- [ ] Imports organized
