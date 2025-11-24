# Button Idle Animations

Bộ animation templates cho Unity UI buttons và titles. Tất cả animations đều **loop liên tục**.

## Animations có sẵn

### 1. **Button_Pulse.anim** ⭐ PHỔ BIẾN NHẤT
- **Mô tả**: Scale từ 1.0 → 1.1 → 1.0 (smooth)
- **Thời gian**: 1 giây
- **Dùng cho**: Buttons quan trọng (Play, Start, Click Here)
- **Hiệu ứng**: Nhẹ nhàng, thu hút sự chú ý

### 2. **Button_ScalePulse.anim** ⚡ CÓ OVERSHOOT
- **Mô tả**: Scale với bounce effect (1.0 → 1.15 → 0.95 → 1.05 → 1.0)
- **Thời gian**: 1 giây
- **Dùng cho**: Call-to-action buttons, promotional buttons
- **Hiệu ứng**: Năng động, có sự sống động

### 3. **Button_Rotate.anim** 🔄 LOADING
- **Mô tả**: Xoay 360 độ liên tục
- **Thời gian**: 2 giây
- **Dùng cho**: Loading indicators, icons
- **Hiệu ứng**: Rotating spinner

### 4. **Button_Float.anim** ☁️ NHẸ NHÀNG
- **Mô tả**: Di chuyển lên xuống 10px (position Y)
- **Thời gian**: 1.5 giây
- **Dùng cho**: Floating buttons, cloud icons, achievement badges
- **Hiệu ứng**: Floating/hovering effect

### 5. **Button_Glow.anim** ✨ ALPHA FADE
- **Mô tả**: Alpha từ 1.0 → 0.5 → 1.0
- **Thời gian**: 1.2 giây
- **Dùng cho**: Notification dots, glow effects
- **Yêu cầu**: Object phải có **CanvasGroup** component
- **Hiệu ứng**: Pulsing glow

## Cách sử dụng

### Phương pháp 1: Dùng Animator (Recommended)

1. **Chọn UI object** (Button, Image, Text) trong Hierarchy
2. **Add Component** → Animator
3. **Assign Controller**: Kéo `ButtonIdle.controller` vào field "Controller"
4. **Chọn animation**: Trong Animator window, set default state:
   - `Idle_Pulse` - Cho pulse effect
   - `Idle_Rotate` - Cho rotate effect
   - `Idle_Float` - Cho float effect
   - `Idle_Glow` - Cho glow effect (cần CanvasGroup)
   - `Idle_None` - Không animation

### Phương pháp 2: Play trực tiếp từ code

```csharp
using UnityEngine;

public class MyButton : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Play animation bằng code
        animator.Play("Idle_Pulse");
    }
}
```

### Phương pháp 3: Dùng Animation component (Đơn giản hơn)

1. **Chọn UI object**
2. **Add Component** → Animation (không phải Animator)
3. **Kéo thả** file .anim vào Animation component
4. **Check** "Play Automatically"

## Tips & Best Practices

### ✅ DOs
- Dùng **Pulse** hoặc **ScalePulse** cho main action buttons
- Dùng **Rotate** cho loading spinners
- Dùng **Float** cho decorative elements
- Dùng **Glow** cho notification indicators
- Combine nhiều animations (ví dụ: Pulse + Glow)

### ❌ DON'Ts
- Đừng dùng animation cho tất cả buttons (chỉ highlight buttons quan trọng)
- Đừng dùng Rotate cho buttons (chỉ dùng cho icons/spinners)
- Đừng combine Float + Rotate (quá nhiều movement)

## Combining Animations

Bạn có thể combine nhiều animations bằng cách:

1. **Tạo Animator Layers**:
   - Layer 1: Scale animation (Pulse/ScalePulse)
   - Layer 2: Glow animation (Glow)

2. **Hoặc dùng 2 objects**:
   ```
   Button (parent)
   ├── Icon (child) - Rotate animation
   └── Glow (child) - Glow animation
   ```

## Customize

Để chỉnh sửa animation trong Unity Editor:

1. **Double-click** file .anim
2. Mở **Animation window** (Window > Animation > Animation)
3. Chỉnh sửa keyframes, timing, curves
4. **Save**

### Các thông số hay chỉnh:

- **Speed**: Scale faster/slower (trong Animator state)
- **Scale amount**: 1.1 → 1.2 cho effect mạnh hơn
- **Float distance**: 10px → 20px cho movement lớn hơn
- **Alpha range**: 0.5 → 0.3 cho glow effect mạnh hơn

## Requirements

- Unity 2020.3 hoặc mới hơn
- UI Toolkit (built-in)
- **Button_Glow** yêu cầu CanvasGroup component

## File Structure

```
ButtonIdle/
├── Button_Pulse.anim          # Scale pulse animation
├── Button_ScalePulse.anim     # Scale với overshoot
├── Button_Rotate.anim         # Rotation animation
├── Button_Float.anim          # Position float animation
├── Button_Glow.anim           # Alpha glow animation
├── ButtonIdle.controller      # Animator controller
└── README.md                  # This file
```

## Examples

### Play Button (Main menu)
```
Component: Animator
Controller: ButtonIdle
Default State: Idle_ScalePulse
```

### Loading Spinner
```
Component: Animator
Controller: ButtonIdle
Default State: Idle_Rotate
```

### Achievement Badge
```
Component: Animator
Controller: ButtonIdle
Default State: Idle_Float
+ Add CanvasGroup + Idle_Glow on separate layer
```

### Notification Dot
```
Component: Animator
Controller: ButtonIdle
Default State: Idle_Glow
Note: Add CanvasGroup component first!
```

---

**Created by**: Claude Code
**Version**: 1.0
**License**: Free to use in your Unity projects
