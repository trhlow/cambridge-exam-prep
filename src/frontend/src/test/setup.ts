import '@testing-library/jest-dom/vitest'

// jsdom has no pointer capture; the drag-and-drop cards call it on pointerdown.
Element.prototype.setPointerCapture ??= () => undefined
Element.prototype.releasePointerCapture ??= () => undefined
