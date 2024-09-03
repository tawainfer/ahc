use svg::node::element::{Circle, Line, Polygon, Rectangle, Title, SVG};
use svg::node::Text;

use crate::{Input, VisData};

const W: f64 = 800.0;
const H: f64 = 800.0;
const PADDING: f64 = 10.0;
const COORD_MAX: f64 = 1000.0;
const VERTEX_SZ: f64 = 5.0;
const RATIO: f64 = H / COORD_MAX;

pub fn new_svg() -> SVG {
    let mut doc = svg::Document::new()
        .set("id", "vis")
        .set(
            "viewBox",
            (-PADDING, -PADDING, W + 2.0 * PADDING, H + 2.0 * PADDING),
        )
        .set("width", W + 2.0 * PADDING)
        .set("height", H + 2.0 * PADDING);

    doc = doc.add(
        Rectangle::new()
            .set("x", -PADDING)
            .set("y", -PADDING)
            .set("width", W + 2.0 * PADDING)
            .set("height", H + 2.0 * PADDING)
            .set("fill", "white")
            .set("stroke-width", "0.0"),
    );

    doc
}

pub fn draw_graph(vis_data: &VisData, input: &Input, mut doc: SVG) -> SVG {
    // edge
    for &(u, v) in input.edges.iter() {
        let (x1, y1) = input.coordinates[u];
        let (x2, y2) = input.coordinates[v];
        doc = draw_line(doc, x1, y1, x2, y2, Some("lightgray"), 0.8, RATIO);
    }

    // vertex
    for (i, &(x, y)) in input.coordinates.iter().enumerate() {
        if vis_data.state.traffic_light[i] == 0 {
            let col = "orangered";
            let sz = 1.0;
            doc = draw_circle(doc, x, y, VERTEX_SZ * sz, col, 1.0, None, 0.0, RATIO)
        } else {
            let col = "mediumseagreen";
            let sz = 1.5;
            doc = draw_circle(doc, x, y, VERTEX_SZ * sz, col, 1.0, None, 0.0, RATIO)
        }
    }

    doc
}

pub fn draw_cur_v(vis_data: &VisData, input: &Input, mut doc: SVG) -> SVG {
    let cur_v = vis_data.state.cur_v;
    let (x, y) = input.coordinates[cur_v];
    doc = draw_circle(doc, x, y, VERTEX_SZ * 1.5, "gray", 0.5, None, 0.0, RATIO);
    draw_circle(
        doc,
        x,
        y,
        VERTEX_SZ * 1.5,
        "none",
        1.0,
        Some("blue"),
        2.0,
        RATIO,
    )
}

pub fn draw_target(vis_data: &VisData, input: &Input, mut doc: SVG) -> SVG {
    let target_idx = vis_data.state.target_idx;
    if target_idx < input.t_sz {
        let idx = input.t[target_idx];
        let (x, y) = input.coordinates[idx];
        doc = draw_star(doc, VERTEX_SZ * 2.5, x as f64, y as f64, RATIO);
    }
    doc
}

// Draw the latest k vertices that have been visited
pub fn draw_visited_vertices(vis_data: &VisData, input: &Input, mut doc: SVG, k: usize) -> SVG {
    let col = "steelblue";
    let n = vis_data.state.visited.len();
    for i in n - k.min(n)..n - 1 {
        let idx = vis_data.state.visited[i];
        let (x, y) = input.coordinates[idx];
        doc = draw_circle(
            doc,
            x,
            y,
            VERTEX_SZ * 1.5,
            "none",
            1.0,
            Some(col),
            1.0,
            RATIO,
        );

        // draw the path
        let (nx, ny) = input.coordinates[vis_data.state.visited[i + 1]];
        doc = draw_line(doc, x, y, nx, ny, Some(col), 1.5, RATIO);
    }
    doc
}

pub fn draw_tooltips(input: &Input, mut doc: SVG) -> SVG {
    for i in 0..input.n {
        let (x, y) = input.coordinates[i];
        doc = draw_rectangle(
            doc,
            x - VERTEX_SZ as i64,
            y - VERTEX_SZ as i64,
            VERTEX_SZ * 2.0,
            VERTEX_SZ * 2.0,
            "gray",
            0.0,
            None,
            0.0,
            RATIO,
            Some(format!("{}-th vertex", i)),
        );
    }
    doc
}

fn draw_rectangle(
    doc: SVG,
    x: i64,
    y: i64,
    width: f64,
    height: f64,
    fill: &str,
    fill_opacity: f64,
    stroke: Option<&str>,
    stroke_width: f64,
    ratio: f64,
    title: Option<String>,
) -> SVG {
    let mut rect = Rectangle::new()
        .set("x", x as f64 * ratio)
        .set("y", y as f64 * ratio)
        .set("width", width * ratio)
        .set("height", height * ratio)
        .set("fill", fill)
        .set("fill-opacity", fill_opacity);
    if let Some(stroke) = stroke {
        rect = rect.set("stroke", stroke).set("stroke-width", stroke_width);
    }

    if let Some(title) = title {
        rect = rect.add(Title::new().add(Text::new(title)));
    }

    doc.add(rect)
}

fn draw_line(
    doc: SVG,
    x1: i64,
    y1: i64,
    x2: i64,
    y2: i64,
    stroke: Option<&str>,
    stroke_width: f64,
    ratio: f64,
) -> SVG {
    let mut line = Line::new()
        .set("x1", x1 as f64 * ratio)
        .set("x2", x2 as f64 * ratio)
        .set("y1", y1 as f64 * ratio)
        .set("y2", y2 as f64 * ratio);

    if let Some(stroke) = stroke {
        line = line.set("stroke", stroke).set("stroke-width", stroke_width);
    }

    doc.add(line)
}

fn draw_circle(
    doc: SVG,
    cx: i64,
    cy: i64,
    r: f64,
    fill: &str,
    fill_opacity: f64,
    stroke: Option<&str>,
    stroke_width: f64,
    ratio: f64,
) -> SVG {
    let mut circle = Circle::new()
        .set("cx", cx as f64 * ratio)
        .set("cy", cy as f64 * ratio)
        .set("r", r * ratio)
        .set("fill", fill)
        .set("fill-opacity", fill_opacity);
    if let Some(stroke) = stroke {
        circle = circle
            .set("stroke", stroke)
            .set("stroke-width", stroke_width);
    }

    doc.add(circle)
}

fn draw_star(doc: SVG, r: f64, cx: f64, cy: f64, ratio: f64) -> SVG {
    // The golden ratio
    // https://doc.rust-lang.org/std/f64/consts/constant.PHI.html
    const PHI: f64 = 1.618033988749894848204586834365638118_f64;
    let theta = 72.0 / 180.0 * std::f64::consts::PI;

    let mut outer_points = vec![];
    for i in 0..5 {
        let start = -90.0 / 180.0 * std::f64::consts::PI;
        let x = cx + r * (start + theta * i as f64).cos();
        let y = cy + r * (start + theta * i as f64).sin();
        outer_points.push((x, y));
    }

    let mut inner_points = vec![];
    for i in 0..5 {
        let start = (-90.0 + 36.0) / 180.0 * std::f64::consts::PI;
        let inner_r = r * PHI * (PHI - 1.0) / (PHI + 1.0);
        let x = cx + inner_r * (start + theta * i as f64).cos();
        let y = cy + inner_r * (start + theta * i as f64).sin();
        inner_points.push((x, y));
    }

    let mut v = vec![];
    for i in 0..5 {
        v.push(outer_points[i]);
        v.push(inner_points[i]);
    }
    let mut points_str = String::new();
    for p in v {
        points_str.push_str(&format!("{},{} ", p.0 * ratio, p.1 * ratio));
    }
    let star = Polygon::new()
        .set("fill", "none")
        .set("stroke", "blue")
        .set("stroke-width", 1.2)
        .set("points", points_str);

    doc.add(star)
}
