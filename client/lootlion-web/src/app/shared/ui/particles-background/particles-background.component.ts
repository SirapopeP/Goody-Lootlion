import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';

/** แปลง #RRGGBB → RGB (fallback เมื่อไม่ใช่ hex) */
function parseHexColor(hex: string): { r: number; g: number; b: number } {
  const m = /^#?([0-9a-fA-F]{6})$/.exec(hex.trim());
  if (!m) {
    return { r: 41, g: 173, b: 201 };
  }
  const n = parseInt(m[1], 16);
  return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
}

class Particle {
  x: number;
  y: number;
  radius: number;
  dx: number;
  dy: number;
  angle: number;
  orbitRadius: number;
  baseAngle: number;
  color: string;

  constructor(color: string, orbitRadiusMin: number, orbitRadiusMax: number) {
    this.color = color;
    const centerX = window.innerWidth / 2;
    const centerY = window.innerHeight / 2;

    this.baseAngle = Math.random() * Math.PI * 2;
    this.angle = this.baseAngle;
    const lo = Math.min(orbitRadiusMin, orbitRadiusMax);
    const hi = Math.max(orbitRadiusMin, orbitRadiusMax);
    this.orbitRadius = lo + Math.random() * (hi - lo || 1);
    this.x = centerX + Math.cos(this.angle) * this.orbitRadius;
    this.y = centerY + Math.sin(this.angle) * this.orbitRadius;

    this.radius = Math.random() * 2 + 1;
    this.dx = Math.cos(this.angle) * 0.1;
    this.dy = Math.sin(this.angle) * 0.1;
  }

  draw(ctx: CanvasRenderingContext2D): void {
    ctx.beginPath();
    ctx.arc(this.x, this.y, this.radius, 0, Math.PI * 2);
    ctx.fillStyle = this.color;
    ctx.shadowColor = this.color;
    ctx.shadowBlur = 10;
    ctx.fill();
    ctx.shadowBlur = 0;
  }

  update(
    ctx: CanvasRenderingContext2D,
    globalAngle: number,
    mouse: { x: number | null; y: number | null }
  ): void {
    const centerX = window.innerWidth / 2;
    const centerY = window.innerHeight / 2;

    this.angle = this.baseAngle + globalAngle;

    const targetX = centerX + Math.cos(this.angle) * this.orbitRadius;
    const targetY = centerY + Math.sin(this.angle) * this.orbitRadius;

    const dx = targetX - this.x;
    const dy = targetY - this.y;

    const dist = Math.sqrt(dx * dx + dy * dy);
    const response = Math.min(0.2, dist / 500);

    this.dx += dx * response;
    this.dy += dy * response;

    const maxSpeed = dist > 100 ? 8 : 1.2;
    const speed = Math.sqrt(this.dx * this.dx + this.dy * this.dy);
    if (speed > maxSpeed) {
      this.dx = (this.dx / speed) * maxSpeed;
      this.dy = (this.dy / speed) * maxSpeed;
    }

    this.x += this.dx;
    this.y += this.dy;

    const friction = dist > 100 ? 0.92 : 0.96;
    this.dx *= friction;
    this.dy *= friction;

    if (mouse.x !== null && mouse.y !== null) {
      const mdx = mouse.x - this.x;
      const mdy = mouse.y - this.y;
      const mouseDist = Math.sqrt(mdx * mdx + mdy * mdy);

      if (mouseDist < 100) {
        this.dx -= mdx * 0.01;
        this.dy -= mdy * 0.01;
      }
    }

    this.draw(ctx);
  }

  adjustPosition(_oldWidth: number, _oldHeight: number): void {
    const centerX = window.innerWidth / 2;
    const centerY = window.innerHeight / 2;
    this.x = centerX + Math.cos(this.angle) * this.orbitRadius;
    this.y = centerY + Math.sin(this.angle) * this.orbitRadius;
  }
}

/**
 * พื้นหลังอนุภาคแบบ canvas (อิงแนวคิดจาก sirapope-schoolie-frontend particles)
 * — ใช้ fixed เต็ม viewport, อยู่หลังเนื้อหา (z-index 0)
 */
@Component({
  selector: 'lootlion-particles-background',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './particles-background.component.html',
  styleUrl: './particles-background.component.css',
})
export class ParticlesBackgroundComponent implements AfterViewInit, OnDestroy {
  @ViewChild('particleCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  /** สีจุดอนุภาค (hex แนะนำ) */
  @Input() color = '#29adc9';

  @Input() particleCount = 48;

  @Input() connectionDistance = 110;

  /** ความทึบของ “หาง” การล้างเฟรม (0–1) */
  @Input() trailAlpha = 0.08;

  /**
   * รัศมีวงโคจรของแต่ละจุด (px จากกลางจอ) — ค่าสุ่มระหว่าง min–max
   * เพิ่ม min/max เพื่อให้ “วง” กว้างขึ้น (เดิมเทียบเท่า ~200–300)
   */
  @Input() orbitRadiusMin = 300;

  @Input() orbitRadiusMax = 400;

  private ctx!: CanvasRenderingContext2D;
  private animationFrameId = 0;
  private particles: Particle[] = [];
  private globalAngle = 0;
  private readonly mouse = { x: null as number | null, y: null as number | null };
  private oldWidth = typeof window !== 'undefined' ? window.innerWidth : 1024;
  private oldHeight = typeof window !== 'undefined' ? window.innerHeight : 768;

  private lineRgb = { r: 41, g: 173, b: 201 };

  private readonly onResize = (): void => this.resizeCanvas();
  private readonly onMouseMove = (e: MouseEvent): void => {
    this.mouse.x = e.clientX;
    this.mouse.y = e.clientY;
  };

  ngAfterViewInit(): void {
    this.lineRgb = parseHexColor(this.color);
    const canvas = this.canvasRef.nativeElement;
    this.ctx = canvas.getContext('2d')!;

    window.addEventListener('resize', this.onResize);
    window.addEventListener('mousemove', this.onMouseMove);

    this.resizeCanvas();

    const count =
      typeof window !== 'undefined' &&
      window.matchMedia('(prefers-reduced-motion: reduce)').matches
        ? Math.max(12, Math.floor(this.particleCount / 2))
        : this.particleCount;

    this.particles = Array.from(
      { length: count },
      () => new Particle(this.color, this.orbitRadiusMin, this.orbitRadiusMax)
    );
    this.animate();
  }

  ngOnDestroy(): void {
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
    }
    window.removeEventListener('resize', this.onResize);
    window.removeEventListener('mousemove', this.onMouseMove);
  }

  private resizeCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) {
      return;
    }
    const newWidth = window.innerWidth;
    const newHeight = window.innerHeight;

    this.particles.forEach((p) => p.adjustPosition(this.oldWidth, this.oldHeight));

    canvas.width = newWidth;
    canvas.height = newHeight;

    this.oldWidth = newWidth;
    this.oldHeight = newHeight;
  }

  private connectLines(): void {
    const { r, g, b } = this.lineRgb;
    const maxD = this.connectionDistance;
    this.ctx.lineWidth = 0.35;

    for (let i = 0; i < this.particles.length; i++) {
      for (let j = i + 1; j < this.particles.length; j++) {
        const dx = this.particles[i].x - this.particles[j].x;
        const dy = this.particles[i].y - this.particles[j].y;
        const dist = Math.sqrt(dx * dx + dy * dy);

        if (dist < maxD) {
          const alpha = 1 - dist / maxD;
          this.ctx.beginPath();
          this.ctx.moveTo(this.particles[i].x, this.particles[i].y);
          this.ctx.lineTo(this.particles[j].x, this.particles[j].y);
          this.ctx.strokeStyle = `rgba(${r},${g},${b},${alpha * 0.55})`;
          this.ctx.stroke();
        }
      }
    }
  }

  private animate = (): void => {
    this.animationFrameId = requestAnimationFrame(this.animate);

    const w = window.innerWidth;
    const h = window.innerHeight;
    this.ctx.clearRect(0, 0, w, h);
    this.ctx.fillStyle = `rgba(0, 0, 0, ${this.trailAlpha})`;
    this.ctx.fillRect(0, 0, w, h);

    this.globalAngle += 0.0005;

    this.particles.forEach((p) => p.update(this.ctx, this.globalAngle, this.mouse));
    this.connectLines();
  };
}
