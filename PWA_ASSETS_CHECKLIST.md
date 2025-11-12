# PWA Assets Checklist - Day 1

## 📋 Required Assets

### Icons (Critical - Needed for PWA)
- [ ] **favicon.ico** (32x32 or 64x64)
  - Location: `smartcost-dashboard/public/favicon.ico`
  - Format: ICO
  - Use: Browser tab icon

- [ ] **logo192.png** (192x192)
  - Location: `smartcost-dashboard/public/logo192.png`
  - Format: PNG with transparency
  - Use: Android home screen, PWA install prompt
  - Purpose: "any" (standard app icon)

- [ ] **logo512.png** (512x512)
  - Location: `smartcost-dashboard/public/logo512.png`
  - Format: PNG with transparency
  - Use: High-res displays, splash screens
  - Purpose: "any" and "maskable" (adaptive icon)

- [ ] **apple-touch-icon.png** (180x180)
  - Location: `smartcost-dashboard/public/apple-touch-icon.png`
  - Format: PNG, no transparency (use white background)
  - Use: iOS home screen icon

### Screenshots (Recommended - Improves PWA)
- [ ] **screenshot-dashboard.png** (1280x720)
  - Location: `smartcost-dashboard/public/screenshot-dashboard.png`
  - Format: PNG
  - Content: Main dashboard view with cost charts
  - Use: Desktop PWA installation preview

- [ ] **screenshot-analytics.png** (1280x720)
  - Location: `smartcost-dashboard/public/screenshot-analytics.png`
  - Format: PNG
  - Content: Analytics/trends page
  - Use: Desktop PWA gallery

- [ ] **screenshot-mobile-dashboard.png** (750x1334)
  - Location: `smartcost-dashboard/public/screenshot-mobile-dashboard.png`
  - Format: PNG (portrait)
  - Content: Mobile dashboard view
  - Use: Mobile PWA installation preview

- [ ] **screenshot-mobile-alerts.png** (750x1334)
  - Location: `smartcost-dashboard/public/screenshot-mobile-alerts.png`
  - Format: PNG (portrait)
  - Content: Alerts/budgets view
  - Use: Mobile PWA gallery

### Social Media (Important - For marketing)
- [ ] **og-image.png** (1200x630)
  - Location: `smartcost-dashboard/public/og-image.png`
  - Format: PNG or JPG
  - Content: Logo + tagline + dashboard preview
  - Use: Open Graph (LinkedIn, Facebook shares)

- [ ] **twitter-image.png** (1200x675)
  - Location: `smartcost-dashboard/public/twitter-image.png`
  - Format: PNG or JPG
  - Content: Similar to og-image, optimized for Twitter
  - Use: Twitter card preview

---

## 🎨 Design Guidelines

### Color Scheme
```
Primary: #0078d4 (Azure blue)
Secondary: #50e6ff (Light blue)
Accent: #00bcf2 (Bright blue)
Background: #ffffff (White)
Text: #323130 (Dark gray)
```

### Logo Design Specs
- **Style**: Modern, clean, professional
- **Elements**: Cloud + Dollar sign or Chart icon
- **Typography**: Sans-serif (Arial, Segoe UI, or similar)
- **Format**: Vector-based, export to PNG

### Icon Anatomy
```
192x192 and 512x512:
┌─────────────────┐
│   16px margin   │  <- Safe area
│  ┌───────────┐  │
│  │           │  │
│  │   LOGO    │  │  <- Main logo centered
│  │           │  │
│  └───────────┘  │
│   16px margin   │
└─────────────────┘
```

---

## 🛠️ Tools Recommendation

### Option 1: Canva (Easiest - Recommended)
1. Go to https://canva.com
2. Use templates:
   - "App Icon" (for logo192/512)
   - "Website Screenshot Mockup" (for screenshots)
   - "Open Graph Image" (for og-image)
3. Export as PNG
4. Resize if needed with https://squoosh.app

### Option 2: Figma (Professional)
1. Create artboards with exact dimensions
2. Design with Azure branding
3. Export as PNG @2x
4. Use Figma's export presets

### Option 3: Quick Placeholder (5 minutes)
```bash
# Use ImageMagick to create solid color placeholders
# Install: choco install imagemagick

convert -size 192x192 xc:#0078d4 -gravity center \
  -pointsize 72 -fill white -annotate +0+0 "SC" \
  smartcost-dashboard/public/logo192.png

convert -size 512x512 xc:#0078d4 -gravity center \
  -pointsize 200 -fill white -annotate +0+0 "SC" \
  smartcost-dashboard/public/logo512.png

convert -size 180x180 xc:#0078d4 -gravity center \
  -pointsize 72 -fill white -annotate +0+0 "SC" \
  smartcost-dashboard/public/apple-touch-icon.png

convert -size 64x64 xc:#0078d4 -gravity center \
  -pointsize 32 -fill white -annotate +0+0 "SC" \
  smartcost-dashboard/public/favicon.ico
```

---

## 📸 Screenshots - How to Capture

### Desktop Screenshots (1280x720)
1. Open dashboard in Chrome
2. Set viewport: 1280x720 (DevTools → Device Toolbar)
3. Load with sample data
4. Use browser screenshot: F12 → Ctrl+Shift+P → "Capture screenshot"
5. Crop to exact size

### Mobile Screenshots (750x1334)
1. Open in Chrome mobile emulator
2. Device: iPhone 8 Plus (or similar 750x1334)
3. Capture full page
4. Show realistic data

### Quick Screenshot Tool
```bash
# Puppeteer script to automate screenshots
npm install -g puppeteer

# Create screenshot.js:
node screenshot.js https://app.smartcost.com 1280x720 screenshot-dashboard.png
```

---

## ✅ Verification Checklist

After creating all assets:
- [ ] All files in correct location (`smartcost-dashboard/public/`)
- [ ] Correct dimensions (use online tool to verify)
- [ ] PNG format with transparency (except apple-touch-icon)
- [ ] File sizes reasonable (<500KB each)
- [ ] Colors match brand guidelines
- [ ] Logo is clear and readable at all sizes
- [ ] Screenshots show realistic, attractive data

---

## 🚀 Next Steps After Assets

Once all assets are created:
1. Run: `npm run build` to verify manifest
2. Test PWA install in Chrome DevTools
3. Run Lighthouse audit (should score 100 on PWA)
4. Deploy to staging for testing
5. ✅ Move to Day 2 tasks

---

## ⏱️ Time Estimate

- Quick placeholders: 15 minutes
- Basic design (Canva): 2-3 hours
- Professional design (Figma): 4-6 hours

**Recommendation**: Start with placeholders to unblock Day 2, refine later.

---

## 💡 Pro Tips

1. **Maskable Icons**: Create a version with 20% padding for Android adaptive icons
2. **Dark Mode**: Consider creating dark variants for iOS
3. **Consistency**: Use same logo/style across all sizes
4. **Testing**: Install PWA on real devices to verify icons look good
5. **Iteration**: You can refine assets after beta launch

---

**Current Status**: ✅ VAPID keys generated, .env.production created
**Next Task**: Create PWA assets (icons + screenshots)
**Blocker**: None - Can use placeholders to continue
