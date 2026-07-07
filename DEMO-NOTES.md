# Minimal APIs vs Controllers — Demo Notes (15-20 phút)

Repo này có cùng 1 domain (Users) được cài đặt song song ở 2 style:
- Minimal API: `/minimal/users` ([Program.cs](Program.cs))
- Controller: `/controller/users` ([Controllers/UsersController.cs](Controllers/UsersController.cs))

Cả 2 dùng chung tầng `IUserService` / `IUserRepository`, nên logic nghiệp vụ giống hệt nhau —
chỉ khác cú pháp khai báo route/filter/auth. Đây là chủ đích: so sánh "táo với táo".

## Agenda (~18 phút)

### 1. Setup & routing (2 phút)
- Chạy app, mở Scalar UI (`/scalar/v1` khi Development).
- Show 2 nhóm route cùng tồn tại, cùng schema request/response.
- Controller: attribute routing (`[Route]`, `[HttpGet]`).
- Minimal: `MapGroup("/minimal/users")` + method chaining ([Program.cs:53](Program.cs#L53)).

### 2. CRUD dùng chung 1 nguồn data (3 phút)
```bash
curl http://localhost:5225/minimal/users
curl http://localhost:5225/controller/users

# Tạo qua Controller, đọc lại qua Minimal -> cùng data vì chung Service/Repository
curl -X POST http://localhost:5225/controller/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Demo","email":"demo@test.com","age":25}'
curl http://localhost:5225/minimal/users
```
Điểm nhấn: khác cú pháp, chung logic nghiệp vụ.

### 3. Validation (2 phút)
```bash
# Thiếu "name" (Required)
curl -X POST http://localhost:5225/minimal/users \
  -H "Content-Type: application/json" -d '{"email":"x@test.com","age":25}'
curl -X POST http://localhost:5225/controller/users \
  -H "Content-Type: application/json" -d '{"email":"x@test.com","age":25}'
```
- Minimal API: `builder.Services.AddValidation()` ([Program.cs:25](Program.cs#L25)) — tính năng mới .NET 10, đọc DataAnnotations tự động.
- Controller: `[ApiController]` tự validate model từ trước đó rất lâu.

### 4. Filters — cùng ý tưởng, khác cách viết (3 phút)
```bash
curl http://localhost:5225/minimal/users
curl http://localhost:5225/controller/users
# xem log terminal: [Minimal] GET ... -> Xms  /  [Controller] GET ... -> Xms
```
- Minimal: `IEndpointFilter` ([Filters/LoggingEndpointFilter.cs](Filters/LoggingEndpointFilter.cs)), gắn qua `.AddEndpointFilter<T>()` trên route group ([Program.cs:53-54](Program.cs#L53-L54)).
- Controller: `IAsyncActionFilter` ([Filters/LoggingActionFilter.cs](Filters/LoggingActionFilter.cs)), gắn qua `options.Filters.Add<T>()` trong `AddControllers` ([Program.cs:15-18](Program.cs#L15-L18)).

### 5. Global exception handling (2 phút)
```bash
curl http://localhost:5225/minimal/users/boom
curl http://localhost:5225/controller/users/boom
```
Cả 2 trả về `ProblemDetails` 500 giống hệt nhau nhờ `IExceptionHandler` dùng chung
([ErrorHandling/ApiExceptionHandler.cs](ErrorHandling/ApiExceptionHandler.cs)) đăng ký 1 lần
qua `app.UseExceptionHandler()` cho toàn bộ app — không phân biệt style.

### 6. Authorization (3 phút)
```bash
curl -i -X DELETE http://localhost:5225/minimal/users/1                                   # 401
curl -i -X DELETE -H "X-Api-Key: demo-secret-key" http://localhost:5225/minimal/users/1   # 204

curl -i -X DELETE http://localhost:5225/controller/users/2                                # 401
curl -i -X DELETE -H "X-Api-Key: demo-secret-key" http://localhost:5225/controller/users/2 # 204
```
- Minimal: `.RequireAuthorization()` trên route builder ([Program.cs:112](Program.cs#L112)).
- Controller: `[Authorize]` trên action ([Controllers/UsersController.cs:72](Controllers/UsersController.cs#L72)).
- Scheme dùng chung: `ApiKeyAuthenticationHandler` so khớp header `X-Api-Key` với config `ApiKey` trong `appsettings.json`.

### 7. Unit testing — có test thật để chạy live (3 phút)

Project [Tests/MinimalAPIs.Tests](Tests/MinimalAPIs.Tests) có 2 bộ test **mirror y hệt nhau** (cùng case: GetById found/not found, Create, Delete found/not found):
- [UsersControllerTests.cs](Tests/MinimalAPIs.Tests/UsersControllerTests.cs) — test Controller.
- [UserEndpointsTests.cs](Tests/MinimalAPIs.Tests/UserEndpointsTests.cs) — test Minimal API.

```bash
dotnet test
```

- **Controller**: là class → `new UsersController(mockService.Object)` rồi gọi thẳng action, assert qua `Assert.IsType<OkObjectResult>(result.Result)`. Pure unit test, không cần host app.
  - Cái bẫy (không xảy ra ở các case demo, nhưng cần biết): action nào đụng `User`/`ModelState`/`Url`/`HttpContext` (property của `ControllerBase`) thì phải tự dựng `controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }` trước, quên là `NullReferenceException`.
- **Minimal API**: ban đầu handler là lambda khai báo inline trong `Program.cs` → không tách được để gọi trực tiếp. Đã refactor tách ra static method trong [Endpoints/UserEndpoints.cs](Endpoints/UserEndpoints.cs) (`UserEndpoints.GetById(int id, IUserService service)`), `Program.cs` chỉ còn `.MapGet("/{id:int}", UserEndpoints.GetById)`.
  - Test gọi thẳng `UserEndpoints.GetById(1, mockService.Object)`, assert qua `Assert.IsType<Ok<User>>(result)` (kiểu cụ thể từ `TypedResults`) — gọn hơn Controller vì không có `ActionResult<T>` wrapper, và không cần dựng `ControllerContext` gì cả vì input là tham số tường minh.
  - Filter (`IEndpointFilter`) nhận `EndpointFilterInvocationContext` dựng tay hơi lằng nhằng hơn, nhưng ASP.NET Core có factory sẵn `EndpointFilterInvocationContext.Create(httpContext, args)` để unit test filter riêng lẻ (không demo trong repo này).
- Cả 2 đều cần integration test (`WebApplicationFactory` + `HttpClient`) để verify routing/filter/validation/auth chạy đúng end-to-end — không khác biệt (repo chưa có, có thể làm thêm nếu cần).

### 8. Feature mapping: Controller → Minimal API (2 phút)

| Tính năng Controller | Minimal API tương đương | Ghi chú |
|---|---|---|
| `[Authorize]` / `[AllowAnonymous]` | `.RequireAuthorization()` / `.AllowAnonymous()` | Đã demo ở mục 6 |
| `[FromBody]`/`[FromQuery]`/`[FromRoute]`/`[FromHeader]` | Cùng attribute, gắn trực tiếp lên tham số delegate | Cùng namespace `Microsoft.AspNetCore.Mvc` |
| `[ApiController]` tự 400 khi model invalid | `AddValidation()` tự short-circuit trả `ProblemDetails` 400 | Minimal API mới có từ .NET 10 |
| 5 loại filter (Authorization/Resource/Action/Exception/Result) | 1 loại duy nhất `IEndpointFilter` (single pipeline) | Ít granular hơn nhưng đủ cho hầu hết use case |
| Areas | Không có khái niệm Areas | Giả lập bằng route prefix + `MapGroup` lồng nhau |
| `[ResponseCache]` | `.CacheOutput()` (Output Caching middleware, .NET 7+) | Middleware dùng chung, chỉ khác cách gắn |
| `[Produces]` / `[Consumes]` | `.Produces<T>()` / `.Accepts<T>()` | Tương đương, phục vụ OpenAPI metadata |
| Custom `IModelBinderProvider` | static `BindAsync(HttpContext)` trên type, hoặc `[AsParameters]` | Cơ chế khác hẳn, cần học lại |
| `Asp.Versioning.Mvc` | `Asp.Versioning.Http` (cùng hãng, API tương tự) | Cả 2 đều được support chính thức |
| `[FromForm] IFormFile` | `IFormFile`/`IFormFileCollection` làm tham số trực tiếp | Tương đương |

### 9. Q&A dự kiến

**Q: Viết Unit Test / Integration Test cho Minimal API có khó hơn mock `ControllerContext` truyền thống không?**
Không khó hơn, mà đổi loại khó khăn: Controller cần dựng `ControllerContext`/`HttpContext` thủ công mới truy cập được `User`/`ModelState`/`Url` (dễ quên, dễ lỗi); Minimal API buộc khai báo input tường minh trên signature nên test bằng cách truyền thẳng object giả, không cần dựng context — nhưng test `IEndpointFilter` riêng lẻ thì lằng nhằng hơn (có `EndpointFilterInvocationContext.Create` hỗ trợ). Integration test qua `WebApplicationFactory` thì giống hệt nhau, chỉ cần thêm `public partial class Program { }`.

**Q: Tại sao cho rằng Minimal API không phù hợp Enterprise Monolith? Có kết hợp được với Clean Architecture không?**
Nhận định đó đúng nếu để tất cả endpoint nằm thẳng trong `Program.cs` — vài trăm endpoint thì file vỡ trận thật, nhưng đó là vấn đề tổ chức code chứ không phải giới hạn kỹ thuật. Minimal API tách được thành extension method theo module/feature (`app.MapUserEndpoints()`...), mỗi module gọi vào Application layer y hệt Controller — Clean Architecture không quan tâm Presentation layer là Controller hay Minimal API, cả hai chỉ là adapter ngoài cùng. Pattern "Minimal API + Vertical Slice + MediatR" đã khá phổ biến cho modular monolith (FastEndpoints, Carter). Điểm thực sự thiếu: chỉ có 1 loại filter thay vì 5 loại filter có thứ tự rõ ràng của MVC, và một số thư viện enterprise cũ (APM, audit) có thể chưa hook được vào Minimal API pipeline. Kết luận: cần thêm kỷ luật kiến trúc (module hoá tường minh) để scale, không phải là không scale được.

### 10. Kết luận (2 phút)

| Khía cạnh | Minimal API | Controller |
|---|---|---|
| Routing | `MapGroup`/`Map*` fluent | Attribute routing |
| Validation | `AddValidation()` (mới, .NET 8+) | `[ApiController]` tự động (lâu đời) |
| Filter | `IEndpointFilter` | `IAsyncActionFilter`/`ActionFilterAttribute` |
| Exception handling | Dùng chung `IExceptionHandler` | Dùng chung `IExceptionHandler` |
| Auth | `.RequireAuthorization()` | `[Authorize]` |
| Unit test | Cần tách handler ra method riêng mới dễ test | Testable "miễn phí" vì là class |
| Boilerplate | Ít hơn cho API nhỏ/gọn | Nhiều hơn nhưng convention rõ ràng, quen thuộc |

**Thông điệp chính:** khác cú pháp, cùng khả năng (feature parity ở .NET 8+) — nhưng không có gì miễn phí, cả 2 đều tốn công wiring như nhau cho filter/auth/exception handling.

### Khuyến nghị: khi nào dùng gì

**Dùng Controller khi:**
- Codebase đã có sẵn nhiều Controller — chi phí chuyển đổi không đáng, giữ nhất quán quan trọng hơn cú pháp gọn.
- Cần nhiều loại filter với thứ tự pipeline rõ ràng (Authorization → Resource → Action → Exception → Result) cho cross-cutting concern phức tạp.
- Phụ thuộc thư viện enterprise cũ (APM, audit log...) chỉ hook được vào MVC filter pipeline, chưa hỗ trợ Minimal API.
- Bề mặt API rất lớn (hàng trăm endpoint), cần convention có sẵn: Areas, custom model binder, versioning theo controller.
- Team ưu tiên testability "mặc định" (là class, mock constructor) hơn là phải chủ động tách handler ra method riêng.

**Dùng Minimal API khi:**
- Service nhỏ/gọn: microservice, BFF, internal tool, cần ít convention, đọc code từ route đến handler trong 1 chỗ.
- Muốn giảm boilerplate và cold-start nhanh hơn (ít reflection/allocation hơn MVC ở benchmark cho case đơn giản).
- Team chủ động tổ chức theo module/feature (extension method như `UserEndpoints.cs` đã làm ở repo này) thay vì cần convention MVC áp sẵn.
- Chấp nhận đánh đổi: phải tự tách handler ra static method nếu muốn unit test dễ, vì không có sẵn như Controller.

**Trường hợp modular monolith / migrate dần:** không cần chọn 1 trong 2 cho cả hệ thống — repo demo này chứng minh cả 2 style dùng chung được Service/Repository layer trong cùng 1 app. Có thể giữ Controller cho phần cũ, dùng Minimal API cho module mới, migrate dần theo từng feature thay vì rewrite toàn bộ.

**Một câu chốt cho slide:** không phải "cái nào tốt hơn" mà là "cái nào khớp với ràng buộc hiện tại của team" — quy mô API, mức độ quen thuộc với MVC, và có bao nhiêu cross-cutting concern cần filter pipeline phức tạp.

## Cheat-sheet đầy đủ

```bash
# CRUD
curl http://localhost:5225/minimal/users
curl http://localhost:5225/controller/users
curl http://localhost:5225/minimal/users/1
curl http://localhost:5225/controller/users/1

curl -X POST http://localhost:5225/minimal/users -H "Content-Type: application/json" -d '{"name":"Demo","email":"demo@test.com","age":25}'
curl -X POST http://localhost:5225/controller/users -H "Content-Type: application/json" -d '{"name":"Demo","email":"demo@test.com","age":25}'

curl -X PUT http://localhost:5225/minimal/users/1 -H "Content-Type: application/json" -d '{"name":"Updated","email":"u@test.com","age":30}'
curl -X PATCH http://localhost:5225/minimal/users/1 -H "Content-Type: application/json" -d '{"age":31}'

# Validation lỗi
curl -X POST http://localhost:5225/minimal/users -H "Content-Type: application/json" -d '{"email":"x@test.com","age":25}'

# Exception handler
curl http://localhost:5225/minimal/users/boom
curl http://localhost:5225/controller/users/boom

# Auth
curl -i -X DELETE http://localhost:5225/minimal/users/1
curl -i -X DELETE -H "X-Api-Key: demo-secret-key" http://localhost:5225/minimal/users/1

# Count (có Stopwatch đo thời gian)
curl "http://localhost:5225/minimal/users/count?age=25"
curl "http://localhost:5225/controller/users/count?age=25"
```
