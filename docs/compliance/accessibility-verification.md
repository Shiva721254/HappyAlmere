# Accessibility verification contract

> HA-010-A documentation contract, 21 November 2026. [Authority](https://github.com/Shiva721254/HappyAlmere/issues/33#issuecomment-7065334379); [phase evidence and dispositions](../phases/HA-010-A.md). Documentation is not production certification, an operational exercise, or permission to process customer data.

PROD-OPEN-04, PROD-T45/46, PROD-RISK14. Engineering target WCAG2.2AA, [W3C Recommendation12December2024](https://www.w3.org/TR/WCAG22/) refreshed2026-11-21. This target is distinct from EAA/Dutch legal applicability and actual conformity; applicable EN301549/other harmonized standard, national implementation, covered service and exemptions require [legal screening](regulatory-applicability.md).

Current source uses Dutch lang, viewport meta, semantic navigation/main/footer, skip link, encoded Razor and mobile CSS. Existing browser tests cover320/375/430/768/1024/1440/1920px and functional/security behavior; those are not exhaustive accessibility tests. No manual screen-reader or professional accessibility result is invented.

Before release test keyboard-only tab/order/skip/focus/traps, screen-reader names/roles/status/read order, meaningful headings/links, visible focus/contrast, errors/instructions, zoom/reflow/text spacing, long Dutch/English text, touch targets, reduced-motion where applicable and sponsored labels. Future auth/MFA/recovery must avoid inaccessible cognitive barriers; payment and native flows need their own coverage.

Manual evidence must identify exact candidate/device/browser/assistive-technology/version, task and expected/actual behavior, tester, screenshots where safe, defect severity and retest. Include keyboard and at least an approved representative screen-reader combination; native VoiceOver/TalkBack/Dynamic Type/real devices at HA007. Automated checks supplement, never replace manual review.

Positive controls demonstrate successful completion; negative checks verify focus/error recovery without hiding content or globally clipping overflow. Record failures and untested functionality; unresolved applicable barriers require remediation or lawful professional assessment, not blanket microenterprise assumptions. No new scanner, browser test, dependency or CI change under HA010-A.
